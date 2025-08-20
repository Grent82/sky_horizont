using System;

namespace SkyHorizont.Domain.Galaxy.Planet
{
    public readonly struct Resources
    {
        public int Organics { get; }
        public int Ore { get; }
        public int Volatiles { get; }

        public Resources(int organics, int ore, int volatiles)
        {
            if (organics < 0 || ore < 0 || volatiles < 0)
                throw new ArgumentException("Resource values cannot be negative.");

            Organics = organics;
            Ore = ore;
            Volatiles = volatiles;
        }

        /// <summary>
        /// Checks if all resource values are zero.
        /// </summary>
        public bool IsEmpty => Organics == 0 && Ore == 0 && Volatiles == 0;

        /// <summary>
        /// Returns the total resource value (sum of Organics, Ore, and Volatiles).
        /// </summary>
        public int Total => Organics + Ore + Volatiles;

        /// <summary>
        /// Scales the resources by a factor, ensuring non-negative results.
        /// </summary>
        /// <param name="factor">Scaling factor (e.g., 0.5 for half).</param>
        /// <returns>A new Resources instance with scaled values.</returns>
        public Resources Scale(double factor)
        {
            if (factor < 0)
                throw new ArgumentException("Scaling factor cannot be negative.", nameof(factor));
            return new Resources(
                (int)(Organics * factor),
                (int)(Ore * factor),
                (int)(Volatiles * factor));
        }

        /// <summary>
        /// Clamps each resource value to a maximum limit.
        /// </summary>
        /// <param name="max">Maximum value for each resource type.</param>
        /// <returns>A new Resources instance with clamped values.</returns>
        public Resources Clamp(int max)
        {
            if (max < 0)
                throw new ArgumentException("Maximum value cannot be negative.", nameof(max));
            return new Resources(
                Math.Min(Organics, max),
                Math.Min(Ore, max),
                Math.Min(Volatiles, max));
        }

        /// <summary>
        /// Adds two Resources instances, ensuring non-negative results.
        /// </summary>
        /// <param name="a">First Resources instance.</param>
        /// <param name="b">Second Resources instance.</param>
        /// <returns>A new Resources instance with summed values.</returns>
         public static Resources operator +(Resources a, Resources b) =>
            new Resources(a.Organics + b.Organics, a.Ore + b.Ore, a.Volatiles + b.Volatiles);

        /// <summary>
        /// Subtracts one Resources instance from another, ensuring non-negative results.
        /// </summary>
        /// <param name="a">Resources to subtract from.</param>
        /// <param name="b">Resources to subtract.</param>
        /// <returns>A new Resources instance with subtracted values.</returns>
        public static Resources operator -(Resources a, Resources b) =>
            new Resources(
                Math.Max(0, a.Organics - b.Organics),
                Math.Max(0, a.Ore - b.Ore),
                Math.Max(0, a.Volatiles - b.Volatiles));

        /// <summary>
        /// Returns a string representation of the Resources instance.
        /// </summary>
        /// <returns>A string in the format "Resources(Organics: X, Ore: Y, Volatiles: Z)".</returns>
        public override string ToString() => $"Resources(Organics: {Organics}, Ore: {Ore}, Volatiles: {Volatiles})";
    }
}