using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Code.Utility
{
    public static class RNG
    {
        private static readonly Random random = new();

        public static bool Chance(double chance) => random.NextDouble() <= chance;
        public static bool ChanceOf100(double chance) => random.NextDouble() <= chance;

        public static int Get(int min, int max) => random.Next(min, max);
        public static float Get(float min, float max) => ((float)random.NextDouble() * (max - min) + max);
        public static double Get(double min, double max) => ((double)random.NextDouble() * (max - min) + max);


        //Extensions
        public static T GetRandomElement<T>(this List<T> objects) => objects[random.Next(0, objects.Count)];
        public static T GetRandomElement<T>(this T[] objects) => objects[random.Next(0, objects.Length)];
        public static T GetRandomElement<T>(this IEnumerable<T> objects) => objects.ElementAtOrDefault(random.Next(0, objects.Count()));
        public static object GetRandomElement(this IEnumerable objects)
        {
            var objectsGeneric = objects.Cast<object>();
            return objectsGeneric.ElementAtOrDefault(random.Next(0, objectsGeneric.Count()));
        }

        public static T GetByChance<T>(this IEnumerable<IChance<T>> possibleValues)
        {
            if (possibleValues.Count() == 0)
                return default;

            float totalChance = possibleValues.Sum(item => item.Chance);
            float randomValue = (float)new Random().NextDouble() * totalChance;

            float accumulatedChance = 0;
            foreach (var possibleValue in possibleValues)
            {
                accumulatedChance += possibleValue.Chance;
                if (randomValue <= accumulatedChance)
                    return possibleValue.Value;
            }

            return possibleValues.Last().Value;
        }
        public static IEnumerable<T> GetManyByChance<T>(this IEnumerable<IChance<T>> possibleValues) => possibleValues.Where(x => ChanceOf100(x.Chance)).Select(x => x.Value);


        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> values) => values.OrderBy(x => Guid.NewGuid());
    }
}
