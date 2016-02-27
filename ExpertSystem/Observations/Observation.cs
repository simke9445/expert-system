using System;

namespace ExpertSystem.Observations
{

    /// <summary>
    /// Observation Class that represents a particular observation
    /// in the system, with it's observed belief and disbelief probabilties
    /// </summary>
    public abstract class Observation
    {
        private static double CacheEmpty = Double.MinValue;

        public string Name { get; set; }

        /// <summary>
        /// Disbelief Measure
        /// </summary>
        public abstract double Md();

        /// <summary>
        /// Belief Measure
        /// </summary>
        public abstract double Mb();

        public abstract string DisplayName();


        protected Observation(string name)
        {
            Name = name;
            ClearCache();
        }


        public override string ToString()
        {
            return DisplayName();
        }

        public void ClearCache()
        {
            MbCache = CacheEmpty;
            MdCache = CacheEmpty;
        }

        public bool IsCached(double mCache)
        {
            return mCache > CacheEmpty;
        }

        public double MbCache { get; set; }


        public virtual double MbCached()
        {
            return IsCached(MbCache) ? MbCache : MbCache = Mb();
        }

        public double MdCache { get; set; }


        public virtual double MdCached()
        {
            return IsCached(MdCache) ? MdCache : MdCache = Md();
        }


        public double Cf()
        {
            return MbCached() - MdCached();
        }

    }
}