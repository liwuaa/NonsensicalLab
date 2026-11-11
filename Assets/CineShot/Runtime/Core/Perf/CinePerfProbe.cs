using Unity.Profiling;

namespace CineShot.Core
{
    public static class CinePerfProbe
    {
        static readonly ProfilerMarker k_Solve = new ProfilerMarker("CineShot.SolveFrame");
        static readonly double[] k_Window = new double[1024];
        static int k_Head;
        static double k_Sum;
        static double k_Peak;

        public static void BeginSolve()
        {
            k_Solve.Begin();
        }

        public static void EndSolve()
        {
            k_Solve.End();
        }

        public static void Record(double ms)
        {
            k_Sum -= k_Window[k_Head];
            k_Sum += ms;
            k_Window[k_Head] = ms;
            k_Head = (k_Head + 1) & 1023;
            if (ms > k_Peak)
                k_Peak = ms;
        }

        public static double AverageMs
        {
            get { return k_Sum / k_Window.Length; }
        }

        public static double PeakMs
        {
            get { return k_Peak; }
        }

        public static void ResetPeak()
        {
            k_Peak = 0.0;
        }
    }
}
