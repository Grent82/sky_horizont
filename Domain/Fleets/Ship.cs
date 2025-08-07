namespace SkyHorizont.Domain.Fleets
{
    public class Ship
    {
        public Guid Id { get; }
        public ShipClass Class { get; }
        public double BaseAttack { get; private set; }
        public double BaseDefense { get; private set; }
        public double CurrentAttack { get; private set; }
        public double CurrentDefense { get; private set; }
        public double CargoCapacity { get; private set; }
        public double Speed { get; private set; }
        public double Cost { get; } // Credits or resources cost

        public Ship(Guid id, ShipClass shipClass, double baseAtk, double baseDef, double cargoCapacity, double speed, double cost)
        {
            Id = id;
            Class = shipClass;
            BaseAttack = baseAtk;
            BaseDefense = baseDef;
            CargoCapacity = cargoCapacity;
            Speed = speed;
            Cost = cost;
            CurrentAttack = baseAtk;
            CurrentDefense = baseDef;
        }

        public void ApplyResearchBonus(double atkPercent, double defPercent)
        {
            CurrentAttack = BaseAttack * (1 + atkPercent);
            CurrentDefense = BaseDefense * (1 + defPercent);
        }

        public void TakeDamage(double incomingAttack)
        {
            // Simple model: defense reduces incoming damage
            double damage = Math.Max(0, incomingAttack - CurrentDefense); // ToDo: add character skill to reduce damage
            // reduce defense durability or combat power accordingly
            CurrentDefense = Math.Max(0, CurrentDefense - damage * 0.5);
            CurrentAttack = Math.Max(0, CurrentAttack - damage * 0.2);
        }
    }
}