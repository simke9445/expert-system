namespace ExpertSystem.Observations
{
    public class Parenthesis : Observation
    {
        private Observation _observation;
        public Parenthesis(Observation observation) : base("")
        {
            _observation = observation;
        }

        public override double Md()
        {
            return _observation.Md();
        }

        public override double Mb()
        {
            return _observation.Mb();
        }

        public override double MbCached()
        {
            return _observation.MbCached();
        }

        public override double MdCached()
        {
            return _observation.MdCached();
        }

        public override string DisplayName()
        {
            return "(" + _observation.DisplayName() + ")";
        }
    }
}