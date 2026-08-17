using System;

namespace FLib
{
    internal static class HashHelpers2
    {
        internal const int HashPrime = 101;
        internal const int MaxPrimeArrayLength = 0x7FEFFFFD;

        private static readonly int[] Primes =
        {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353,
            431, 521, 631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861,
            5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627,
            52361, 62851, 75431, 90523, 108631, 130363, 156437, 187751, 225307, 270371,
            324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471,
            7199369, 8639249, 10367099, 12440503, 14928601, 17914321, 21497189, 25796633,
            30955957, 37147151, 44576581, 53491939, 64190351, 77028431, 92434111, 110920927,
            133105117, 159726143, 191671381, 230005843, 276006997, 331208387, 397450069,
            476940907, 572329117, 686794937, 824153929, 988984717, 1186781657, 1424137981,
            1708965577, 2050758691, 2146435069
        };

        internal static int GetPrime(int minimum)
        {
            if (minimum < 0)
                throw new ArgumentOutOfRangeException(nameof(minimum));

            for (var i = 0; i < Primes.Length; i++)
            {
                var prime = Primes[i];
                if (prime >= minimum)
                    return prime;
            }

            for (long candidate = minimum | 1L; candidate <= MaxPrimeArrayLength; candidate += 2)
            {
                if ((candidate - 1) % HashPrime != 0 && IsPrime((int)candidate))
                    return (int)candidate;
            }

            return minimum;
        }

        internal static int ExpandPrime(int oldSize)
        {
            var newSize = 2L * oldSize;
            if (newSize > MaxPrimeArrayLength && oldSize < MaxPrimeArrayLength)
                return MaxPrimeArrayLength;

            return GetPrime((int)Math.Min(newSize, int.MaxValue));
        }

        private static bool IsPrime(int candidate)
        {
            if ((candidate & 1) == 0)
                return candidate == 2;

            var limit = (int)Math.Sqrt(candidate);
            for (var divisor = 3; divisor <= limit; divisor += 2)
            {
                if (candidate % divisor == 0)
                    return false;
            }

            return true;
        }
    }
}
