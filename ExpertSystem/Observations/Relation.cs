namespace ExpertSystem.Observations
{

    /// <summary>
    /// Binary Relation Observation, such as And, Or etc
    /// </summary>
    public abstract class Relation : Observation
    {
        protected Observation Left, Right;

        protected Relation(Observation left, Observation right, string name) : base(name)
        {
            Left = left;
            Right = right;
        }

        public override string DisplayName()
        {
            return Left.DisplayName() + " " + Name + " " + Right.DisplayName();
        }

        public override double MdCached()
        {
            return Md();
        }

        public override double MbCached()
        {
            return Mb();
        }
    }
}