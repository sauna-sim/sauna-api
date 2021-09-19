namespace VatsimAtcTrainingSimulator.Core.Data.Procedures.Approaches
{
    public class ApproachMinimums
    {
        public int CatAMinimums { get; set; }
        public int CatBMinimums { get; set; }
        public int CatCMinimums { get; set; }
        public int CatDMinimums { get; set; }
        public int CatEMinimums { get; set; }

        public ApproachMinimums(int catA, int catB, int catC, int catD, int catE)
        {
            CatAMinimums = catA;
            CatBMinimums = catB;
            CatCMinimums = catC;
            CatDMinimums = catD;
            CatEMinimums = catE;
        }

        public int GetMinimums(int finalApproachKias)
        {
            if (finalApproachKias <= 90)
            {
                return CatAMinimums;
            }

            if (finalApproachKias <= 120)
            {
                return CatBMinimums;
            }

            if (finalApproachKias <= 140)
            {
                return CatCMinimums;
            }

            if (finalApproachKias <= 165)
            {
                return CatDMinimums;
            }

            return CatEMinimums;
        }
    }
}
