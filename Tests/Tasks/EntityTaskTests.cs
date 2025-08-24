using System.Runtime.Serialization;
using FluentAssertions;
using Xunit;

using SkyHorizont.Domain.Entity;
using SkyHorizont.Domain.Entity.Task;
using SkyHorizont.Domain.Services;
using SkyHorizont.Domain.Shared;
using TaskStatus = SkyHorizont.Domain.Entity.Task.TaskStatus;

namespace SkyHorizont.Tests.Entity
{
    public class EntityTaskUpdatedTests
    {
        public static readonly Guid Target = Guid.NewGuid();

        // ---------- Constructor & initial state ----------
        [Fact]
        public void NewTask_HasPendingStatus_ZeroProgress_NoCompletionDate()
        {
            var t = new TestTask(successOnFinish: true, rewardMerit: 42);

            t.Id.Should().NotBe(Guid.Empty);
            t.Type.Should().Be(TaskType.Research);
            t.TargetId.Should().Be(Target);
            t.Status.Should().Be(TaskStatus.Pending);
            t.Progress.Should().Be(0.0);
            t.CompletedYear.Should().BeNull();
            t.CompletedMonth.Should().BeNull();
            t.FailureReason.Should().BeNull();
            t.RewardMerit.Should().Be(42);
        }

        // ---------- AssignTo ----------
        [Fact]
        public void AssignTo_Succeeds_WhenCharacterCanPerform_TaskBecomesActive_AndHookRuns()
        {
            var t = new TestTask(successOnFinish: true);
            var chr = Characters.CanResearch();

            var ok = chr.AssignTo(t);
            ok.Should().BeTrue();

            t.Status.Should().Be(TaskStatus.Active);
            t.AssignedCharacterId.Should().Be(chr.Id);
            t.HookAssignedCount.Should().Be(1);
        }

        [Fact]
        public void AssignTo_Fails_WhenCharacterCannotPerform()
        {
            var t = new TestTask(successOnFinish: true);
            var chr = Characters.CannotResearch();

            var ok = chr.AssignTo(t);
            ok.Should().BeFalse();

            t.Status.Should().Be(TaskStatus.Pending);
            t.HookAssignedCount.Should().Be(0);
        }

        // ---------- Tick guards ----------
        [Fact]
        public void Tick_WhenNotActive_Throws()
        {
            var t = new TestTask(successOnFinish: true);
            var clock = new StubClock(3001, 7);
            var rng = new StubRng();
            var sink = new CountingSink();

            Action act = () => t.Tick(delta: 1.0, clock, rng, sink, _ => Characters.CanResearch());
            act.Should().Throw<DomainException>().WithMessage("Task is not active.");
        }

        [Fact]
        public void Tick_FinishButAssignedCharacterMissing_Throws_AndStatusStaysActive()
        {
            var t = new TestTask(successOnFinish: true, speedFactor: 1.0); // finish in one tick
            var chr = Characters.CanResearch();
            chr.AssignTo(t).Should().BeTrue();

            var clock = new StubClock(3001, 7);
            var rng = new StubRng();
            var sink = new CountingSink();

            Action act = () => t.Tick(1.0, clock, rng, sink, _ => null); // performer missing
            act.Should().Throw<DomainException>()
               .WithMessage("Assigned character no longer available.");

            t.Status.Should().Be(TaskStatus.Active);
            t.Progress.Should().BeApproximately(1.0, 1e-9);
        }

        // ---------- Tick finishing: success path (CreateEffect = null) ----------
        [Fact]
        public void Tick_FinishesWithSuccess_SetsDate_NoFailure_PublishesNoEffect_WhenCreateEffectReturnsNull()
        {
            var t = new TestTask(successOnFinish: true, speedFactor: 1.0, createEffect: TestTask.EffectMode.Null);
            var chr = Characters.CanResearch();
            chr.AssignTo(t).Should().BeTrue();

            var clock = new StubClock(3002, 5);
            var rng = new StubRng();
            var sink = new CountingSink();

            t.Tick(1.0, clock, rng, sink, _ => chr);

            t.Status.Should().Be(TaskStatus.Success);
            t.CompletedYear.Should().Be(3002);
            t.CompletedMonth.Should().Be(5);
            t.FailureReason.Should().BeNull();
            t.HookFinishedWithSuccess.Should().BeTrue();

            sink.PublishCount.Should().Be(0);
            chr.Merit.Should().BeGreaterThanOrEqualTo(t.RewardMerit);
        }

        // ---------- Tick finishing: failure path (CreateEffect = non-null) ----------
        [Fact]
        public void Tick_FinishesWithFailure_SetsFailureReason_AndPublishesEffect_AndNoMeritGain()
        {
            var t = new TestTask(successOnFinish: false, speedFactor: 1.0,
                                 createEffect: TestTask.EffectMode.NonNull, failureReason: "bad luck");
            var chr = Characters.CanResearch();
            chr.AssignTo(t).Should().BeTrue();

            var clock = new StubClock(3003, 6);
            var rng = new StubRng();
            var sink = new CountingSink();

            t.Tick(1.0, clock, rng, sink, _ => chr);

            t.Status.Should().Be(TaskStatus.Failed);
            t.CompletedYear.Should().Be(3003);
            t.CompletedMonth.Should().Be(6);
            t.FailureReason.Should().Be("bad luck");
            t.HookFinishedWithSuccess.Should().BeFalse();

            sink.PublishCount.Should().Be(1);
            sink.LastEffect.Should().NotBeNull();

            chr.Merit.Should().Be(0); // reward only on success
        }

        // ---------- Abort behavior ----------
        [Fact]
        public void Abort_FromPending_AndFromActive_SetsAborted_ButIgnoredAfterSuccess()
        {
            // pending → aborted
            var t1 = new TestTask(successOnFinish: true);
            t1.Status.Should().Be(TaskStatus.Pending);
            t1.Abort();
            t1.Status.Should().Be(TaskStatus.Aborted);

            // active → aborted
            var t2 = new TestTask(successOnFinish: true);
            var chr = Characters.CanResearch();
            chr.AssignTo(t2).Should().BeTrue();
            t2.Abort();
            t2.Status.Should().Be(TaskStatus.Aborted);

            // completed → abort should not change
            var t3 = new TestTask(successOnFinish: true, speedFactor: 1.0);
            chr = Characters.CanResearch();
            chr.AssignTo(t3).Should().BeTrue();
            var clock = new StubClock(3004, 1);
            var rng = new StubRng();
            var sink = new CountingSink();
            t3.Tick(1.0, clock, rng, sink, _ => chr);
            t3.Status.Should().Be(TaskStatus.Success);
            t3.Abort();
            t3.Status.Should().Be(TaskStatus.Success);
        }

        // ---------- SpeedFactor: override + base ----------
        [Fact]
        public void SpeedFactor_DefaultBaseIs001_AndOverrideIsUsed()
        {
            var baseTask = new TestTask(successOnFinish: true); // uses base (0.01)
            baseTask.ExposeBaseSpeed(new StubRng()).Should().BeApproximately(0.01, 1e-9);

            var fastTask = new TestTask(successOnFinish: true, speedFactor: 0.25);
            fastTask.ExposeEffectiveSpeed(new StubRng()).Should().BeApproximately(0.25, 1e-9);
        }
    }

    // ======== stubs & helpers ========

    internal sealed class StubClock : IGameClockService
    {
        public int CurrentYear { get; private set; }
        public int CurrentMonth { get; private set; }
        public int MonthsPerYear => 12;

        public StubClock(int year, int month)
        {
            CurrentYear = year;
            CurrentMonth = month;
        }

        public void AdvanceTurn() { /* not used */ }
    }

    internal sealed class StubRng : IRandomService
    {
        private readonly double _v;

        public StubRng(double v = 0.5) => _v = v;

        public double NextDouble() => _v;

        // optional overloads if your IRandomService defines them:
        public double NextDouble(double minInclusive, double maxInclusive)
        {
            var x = _v;
            if (x < minInclusive) x = minInclusive;
            if (x > maxInclusive) x = maxInclusive;
            return x;
        }

        public int NextInt(int minInclusive, int maxExclusive) => (minInclusive + maxExclusive) / 2;
        public int CurrentSeed => 0;
        public void Reseed(int seed) { }
    }

    internal sealed class CountingSink : ITaskEffectSink
    {
        public int PublishCount { get; private set; }
        public TaskEffect? LastEffect { get; private set; }

        public void Publish(TaskEffect effect)
        {
            PublishCount++;
            LastEffect = effect;
        }
    }

    internal static class Characters
    {
        public static Character CanResearch()
        {
            var skills = new SkillSet(Military: 0, Intelligence: 0, Research: 80, Economy: 0);
            var pers = new Personality(50, 50, 50, 50, 50);
            return new Character(Guid.NewGuid(), "Able", 30, 2990, 1, Sex.Male, pers, skills);
        }

        public static Character CannotResearch()
        {
            var skills = new SkillSet(Military: 0, Intelligence: 0, Research: 40, Economy: 0);
            var pers = new Personality(50, 50, 50, 50, 50);
            return new Character(Guid.NewGuid(), "Baker", 22, 3000, 1, Sex.Male, pers, skills);
        }
    }

    // A concrete test task that lets us control:
    //  - success/failure
    //  - effect emission (null or non-null)
    //  - speed factor
    internal sealed class TestTask : EntityTask
    {
        public enum EffectMode { Null, NonNull }

        private readonly bool _successOnFinish;
        private readonly double? _speedFactor;
        private readonly EffectMode _effectMode;
        private readonly string? _failureReason;

        public int HookAssignedCount { get; private set; }
        public bool HookFinishedWithSuccess { get; private set; }

        public TestTask(
            bool successOnFinish,
            int rewardMerit = 10,
            double? speedFactor = null,
            EffectMode createEffect = EffectMode.Null,
            string? failureReason = null)
            : base(Guid.NewGuid(), TaskType.Research, EntityTaskUpdatedTests.Target, rewardMerit)
        {
            _successOnFinish = successOnFinish;
            _speedFactor = speedFactor;
            _effectMode = createEffect;
            _failureReason = failureReason;
        }

        protected override bool EvaluateSuccess(Character chr, IRandomService rng, out string? failureReason)
        {
            failureReason = _successOnFinish ? null : (_failureReason ?? "failed");
            return _successOnFinish;
        }

        protected override TaskEffect? CreateEffect(Character chr, bool success, IGameClockService clock, IRandomService rng)
        {
            if (_effectMode == EffectMode.Null) return null;
            return new DummyEffect(Guid.NewGuid(), chr.Id, clock.CurrentYear, clock.CurrentMonth);
        }

        protected override void OnAssigned(Character chr) => HookAssignedCount++;
        protected override void OnFinished(Character chr, bool success) => HookFinishedWithSuccess = success;

        protected override double SpeedFactor(IRandomService rng)
            => _speedFactor ?? base.SpeedFactor(rng);

        // helpers for testing speed
        public double ExposeBaseSpeed(IRandomService rng) => base.SpeedFactor(rng);
        public double ExposeEffectiveSpeed(IRandomService rng) => SpeedFactor(rng);
    }

    internal sealed record DummyEffect(Guid TaskId, Guid CharacterId, int CompletedYear, int CompletedMonth)
        : TaskEffect(TaskId, CharacterId, CompletedYear, CompletedMonth);
}
