using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using Gurobi;
using System.Linq;
using System.Text.RegularExpressions;
using static Gurobi.GRB;

namespace VaccineRouting
{
    internal static class Program
    {
        // FILES
        private static readonly string DataDir = "origindata";
        private static readonly string OutputDir = "output";
        private static readonly string F_q = Path.Combine(DataDir, "qit_20_simple.txt");
        private static readonly string F_c = Path.Combine(DataDir, "cil_20.txt");
        private static readonly string F_d = Path.Combine(DataDir, "djl_20.txt");
        private static readonly string F_v = Path.Combine(DataDir, "vit_20_simple.txt");
        private static readonly string F_n = Path.Combine(DataDir, "nis_20.txt");

        // ── Equity parameter for the D-Index ( 0 ≤ D_BOUND ≤ 1 ) ────────────────
        private const double D_BOUND = 0.25;

        // =====================================================================
        //                      GLOBAL CONSTANTS  (Phase‑1)
        // =====================================================================

        private const int P1_T = 4;      // weeks
        private const int P1_I = 139;    // population groups
        private const int P1_L = 21;     // candidate stopping locations
        private const int P1_J = 21;     // origin nodes
        private const int P1_S = 3;      // demand scenarios
        private const int P1_K = 139;    // duplicate index set for Gini matrix
        private const int P1_theta = 6;  // utility multiplier
        private static readonly int[] P1_v2 = { 500, 500 };
        private static readonly double[] Prob = { 1.0 / 3, 1.0 / 3, 1.0 / 3 };

        // ---------- solver time limits (seconds) ----------

        private const double P1_TimeLimit = 300;   // Phase‑1
        private const double P2_TimeLimit = 300;   // Phase‑2
        private const double P3_TimeLimit = 300;   // Phase-3

        // =====================================================================
        //                       GLOBAL CONSTANTS  (Phase‑2)
        // =====================================================================
        private const int P2_T = 3;   // operating days in a week
        private const int P2_M = 2;   // mobile facilities
        private const int P2_L = P1_L;
        private const int P2_J = P1_J;
        private const int P2_S = P1_S;
        private const int P2_r = P2_L - 1;  // MTZ big‑M
        private const int StopServiceMin = 30;
        private const int DayBudgetMin = 480;

        // =====================================================================
        //                       GLOBAL CONSTANTS  (Phase-3)
        // =====================================================================
        private const int P3_T = 12;          // 12 day horizon
        private const int P3_M = P2_M;        // reuse 2 vehicles
        private const int P3_L = P2_L;
        private const int P3_J = P2_J;
        private const int P3_S = P2_S;
        private const int P3_I = P1_I;
        private const int P3_K = P1_K;
        private const int P3_theta = P1_theta;
        private const int P3_StopMin = 30;    // minutes per stop
        private const int P3_DayLimit = 480;   // minutes per route

        // ---------- full-resolution data files (12 day horizon) --------------
        private static readonly string F_qFull = Path.Combine(DataDir, "qit_20.txt");
        private static readonly string F_vFull = Path.Combine(DataDir, "vit_20.txt");

        // =====================================================================
        //                                    MAIN
        // =====================================================================
        private static void Main()
        {
            Directory.CreateDirectory(OutputDir);
            Console.WriteLine("==== Integrated Vaccine Routing – FULL PIPELINE ===\n");

            // 1 ── LOAD master matrices
            Console.Write("Loading data sets … ");
            var qMat = LoadIntMatrix(F_q, 3038, 84);
            var cMat = LoadIntMatrix(F_c, 3038, P1_L);
            var dMat = LoadDblMatrix(F_d, P1_L, P1_L);
            var vMat = LoadIntMatrix(F_v, 3038, 84);
            var nMat = LoadIntMatrix(F_n, 3038, P1_S);
            var qMatFull = LoadIntMatrix(F_qFull, 3038, 84);
            var vMatFull = LoadIntMatrix(F_vFull, 3038, 84);
            Console.WriteLine("done.\n");

            // PHASE‑1  (weekly demand / location selection)
            var p1 = SolvePhase1(qMat, cMat, dMat, vMat, nMat);
            Console.WriteLine($"Phase 1 objective = {p1.ObjValue:F4}\n");

            // FOR EACH WEEK: derive sub‑matrices and run PHASE‑2
            for (int w = 0; w < P1_T; w++)
            {
                BuildWeekData(p1, w, cMat, nMat,
                              out var grpIds, out var cilW, out var nisW, out var allow);

                Console.WriteLine($"Week {w + 1}:  |G| = {grpIds.Count},  |allowed L| = {allow.Length}");

                string outFile = Path.Combine(OutputDir, $"week{w + 1}.txt");

                SolvePhase2(w, grpIds, cilW, nisW, dMat, allow, outFile);
            }

            Console.WriteLine("\nAll weeks processed – see ./output/week*.txt");

            // PHASE‑3
            Console.WriteLine("\n=== Phase-3  (integrated 12-day plan) ===");
            SolvePhase3(qMatFull, cMat, dMat, vMatFull, nMat);
            Console.WriteLine("\n=== Pipeline ended. ===");
        }

        // MATRIX READERS

        private static int[,] LoadIntMatrix(string path, int maxR, int maxC)
        {
            var m = new int[maxR, maxC];
            using var sr = new StreamReader(path);
            string? ln; int r = -1;
            while ((ln = sr.ReadLine()) != null)
            {
                if (++r == 0) continue; 
                var tk = ln.Split('\t', StringSplitOptions.TrimEntries);
                for (int c = 1; c < tk.Length; c++)
                    if (!string.IsNullOrWhiteSpace(tk[c]))
                        m[r - 1, c - 1] = int.Parse(tk[c], CultureInfo.InvariantCulture);
            }
            return m;
        }

        private static double[,] LoadDblMatrix(string path, int maxR, int maxC)
        {
            var m = new double[maxR, maxC];
            using var sr = new StreamReader(path);
            string? ln; int r = -1;
            while ((ln = sr.ReadLine()) != null)
            {
                if (++r == 0) continue;
                var tk = ln.Split('\t', StringSplitOptions.TrimEntries);
                for (int c = 1; c < tk.Length; c++)
                    if (!string.IsNullOrWhiteSpace(tk[c]))
                        m[r - 1, c - 1] = double.Parse(tk[c], CultureInfo.InvariantCulture);
            }
            return m;
        }

        // PHASE‑1  –  WEEKLY SIZE / LOCATION MODEL 

        private sealed record Phase1Result(double ObjValue,
                                           double[,,] Wval,
                                           double[,] Aval);

        private static Phase1Result SolvePhase1(int[,] q, int[,] c, double[,] d,
                                                int[,] v, int[,] n)
        {
            using var env = new GRBEnv();
            using var m = new GRBModel(env) { ModelName = "Phase1" };
            m.Parameters.TimeLimit = P1_TimeLimit;

            // ---------------- VARIABLES -------------------------

            var W = new GRBVar[P1_I, P1_T, P1_S];
            for (int i = 0; i < P1_I; i++)
                for (int t = 0; t < P1_T; t++)
                    for (int s = 0; s < P1_S; s++)
                        W[i, t, s] = m.AddVar(0, 400, 0, GRB.INTEGER, $"W_{i}_{t}_{s}");

            var A = new GRBVar[P1_L, P1_T];
            for (int l = 0; l < P1_L; l++)
                for (int t = 0; t < P1_T; t++)
                    A[l, t] = m.AddVar(0, 1, 0, GRB.BINARY, $"A_{l}_{t}");

            var UU = new GRBVar[P1_I];
            for (int i = 0; i < P1_I; i++) UU[i] = m.AddVar(0, 1, 0, GRB.CONTINUOUS, $"UU_{i}");

            var tslref = m.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "tslref");
            var tsltc = m.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "tsltc");
            var E = m.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "E");

            // ── Inactive helper vars, only for completeness ────────────
            var zz1 = m.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "zz1");
            var zz2 = m.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "zz2");
            var zz3 = m.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "zz3");
            var zz4 = m.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "zz4");
            var AvgU = m.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "AvgU");

            // ---------- D-Index auxiliary variables ----------------------------------
            var F = new GRBVar[P1_I];                 // |Uavg – Ui|
            for (int i = 0; i < P1_I; i++)
                F[i] = m.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, $"F_{i}");

            // re-use the “AvgU” variable that is already declared earlier
            GRBVar Uavg = AvgU;


            // Gini helper matrix (commented‑out constraints later)
            /*
            var B = new GRBVar[P1_I, P1_K];
            for (int i = 0; i < P1_I; i++)
                for (int k = 0; k < P1_K; k++)
                    B[i, k] = m.AddVar(0, 1, 0, GRB.CONTINUOUS, $"B_{i}_{k}");
            */

            // ---------------- HELPER arrays ----------------------------------

            //int[] alpha = Enumerable.Repeat(1, 69).Concat(Enumerable.Repeat(2, 75)).ToArray();
            int[] alpha = Enumerable
                 .Repeat(1, 68)
                 .Concat(Enumerable.Repeat(2, 71))
                 .ToArray();

            double[] vmax = new double[P1_I];                // v_max(i)
            for (int i = 0; i < P1_I; i++)
                for (int t = 0; t < P1_T; t++)
                    vmax[i] = Math.Max(vmax[i], v[i, t]);

            // ---------- εi (population shares)  --------------------------------------
            double[] eps = new double[P1_I];
            double totPop = 0;
            for (int i = 0; i < P1_I; i++)
                for (int s = 0; s < P1_S; s++) totPop += n[i, s] * Prob[s];

            for (int i = 0; i < P1_I; i++)
            {
                double sum = 0;
                for (int s = 0; s < P1_S; s++) sum += n[i, s] * Prob[s];
                eps[i] = sum / totPop;
            }

            // ---------------- CONSTRAINTS ------------------------------------
            // Capacity per week
            for (int t = 0; t < P1_T; t++)
                for (int s = 0; s < P1_S; s++)
                {
                    var lhs = new GRBLinExpr();
                    for (int i = 0; i < P1_I; i++) lhs.AddTerm(1, W[i, t, s]);
                    for (int l = 0; l < P1_L; l++) lhs.AddTerm(58.6, A[l, t]);
                    m.AddConstr(lhs, GRB.LESS_EQUAL, 2708.4, $"cap_{t}_{s}");
                }

            // Late‑fraction constraints
            AddLateFraction(m, W, alpha, v, n, Prob, 1, 0.2);
            AddLateFraction(m, W, alpha, v, n, Prob, 2, 0.1);

            // Time‑since‑late equations
            AddTSL(m, W, alpha, n, Prob, 1, tsltc, "tsltc_eq");
            AddTSL(m, W, alpha, n, Prob, 2, tslref, "tslref_eq");

            // Utility max‑min
            for (int i = 0; i < P1_I; i++)
            {
                var util = new GRBLinExpr();
                var lhs = new GRBLinExpr();
                lhs.AddTerm(1, E);
                lhs.AddTerm(-1, UU[i]);

                for (int s = 0; s < P1_S; s++) util.AddTerm(P1_theta * n[i, s], UU[i]);
                for (int t = 0; t < P1_T; t++)
                    for (int s = 0; s < P1_S; s++)
                    {
                        util.AddTerm(-P1_theta, W[i, t, s]);
                        util.AddTerm(v[i, t], W[i, t, s]);
                    }
                m.AddConstr(util, GRB.EQUAL, 0, $"util_{i}");
                m.AddConstr(lhs, GRB.LESS_EQUAL, 0, $"maxmin_{i}");
            }

            // ---------- D-Index linearisation  ---------------------------------------
            // 1. Uavg = Σ εi · Ui
            var avgEq = new GRBLinExpr();
            for (int i = 0; i < P1_I; i++) avgEq.AddTerm(eps[i], UU[i]);
            avgEq.AddTerm(-1, Uavg);
            m.AddConstr(avgEq, GRB.EQUAL, 0, "Uavg_def");

            // 2. |Ui – Uavg|  →  Fi
            for (int i = 0; i < P1_I; i++)
            {
                var c1 = new GRBLinExpr();      // Ui – Uavg ≤ Fi
                c1.AddTerm(1, UU[i]); c1.AddTerm(-1, Uavg); c1.AddTerm(-1, F[i]);
                m.AddConstr(c1, GRB.LESS_EQUAL, 0, $"abs1_{i}");

                var c2 = new GRBLinExpr();      // Uavg – Ui ≤ Fi
                c2.AddTerm(1, Uavg); c2.AddTerm(-1, UU[i]); c2.AddTerm(-1, F[i]);
                m.AddConstr(c2, GRB.LESS_EQUAL, 0, $"abs2_{i}");
            }

            // 3. Σ εi Fi ≤ 2 · D_BOUND · Uavg
            var dLhs = new GRBLinExpr();
            for (int i = 0; i < P1_I; i++) dLhs.AddTerm(eps[i], F[i]);
            var dRhs = new GRBLinExpr(); dRhs.AddTerm(2 * D_BOUND, Uavg);
            m.AddConstr(dLhs, GRB.LESS_EQUAL, dRhs, "Dindex");


            // Demand cannot exceed nis
            for (int i = 0; i < P1_I; i++)
                for (int s = 0; s < P1_S; s++)
                {
                    var lhs = new GRBLinExpr();
                    for (int t = 0; t < P1_T; t++) lhs.AddTerm(1, W[i, t, s]);
                    m.AddConstr(lhs, GRB.LESS_EQUAL, n[i, s], $"dem_{i}_{s}");
                }

            // Location‑dependent capacity
            for (int i = 0; i < P1_I; i++)
                for (int t = 0; t < P1_T; t++)
                    for (int s = 0; s < P1_S; s++)
                    {
                        var lhs = new GRBLinExpr();
                        lhs.AddTerm(1, W[i, t, s]);
                        for (int l = 0; l < P1_L; l++) lhs.AddTerm(-c[i, l] * n[i, s], A[l, t]);
                        m.AddConstr(lhs, GRB.LESS_EQUAL, 0, $"loc_{i}_{t}_{s}");
                    }
            /* ────────────────────────────────────────────────────────────────
            capacity link to q(i,t)  ─ ensures  W[i,t,s] ≤ n[i,s]·q[i,t]
            ---------------------------------------------------------------- */
            for (int i = 0; i < P1_I; i++)
                for (int t = 0; t < P1_T; t++)
                    for (int s = 0; s < P1_S; s++)
                    {
                        var lhs = new GRBLinExpr();
                        lhs.AddTerm(1, W[i, t, s]);
                        m.AddConstr(lhs, GRB.LESS_EQUAL,
                                    n[i, s] * q[i, t],
                                    $"dem_cap_{i}_{t}_{s}");
                    }


            // ***OPTIONAL*** Fairness constraint – commented in originals
            //m.AddConstr(E, GRB.GREATER_EQUAL, 0.7, "fair_E_ge_0.7");

            // ***OPTIONAL*** Gini constraints – fully present but commented‑out
            /*
            for (int i = 0; i < P1_I; i++)
                for (int k = 0; k < P1_K; k++)
                {
                    var lhs1 = new GRBLinExpr(); lhs1.AddTerm(1, UU[i]); lhs1.AddTerm(-1, UU[k]); lhs1.AddTerm(-1, B[i, k]);
                    m.AddConstr(lhs1, GRB.LESS_EQUAL, 0, $"gini1_{i}_{k}");

                    var lhs2 = new GRBLinExpr(); lhs2.AddTerm(1, UU[k]); lhs2.AddTerm(-1, UU[i]); lhs2.AddTerm(-1, B[i, k]);
                    m.AddConstr(lhs2, GRB.LESS_EQUAL, 0, $"gini2_{i}_{k}");
                }
            var gSum = new GRBLinExpr();
            for (int i = 0; i < P1_I; i++)
                for (int k = 0; k < P1_K; k++) gSum.AddTerm(1, B[i, k]);
            for (int i = 0; i < P1_I; i++) gSum.AddTerm(-83.4, UU[i]);
            m.AddConstr(gSum, GRB.LESS_EQUAL, 0, "gini3");
            */

            // ---------------- OBJECTIVE (2*tslref + tsltc) --------------------
            //var obj = new GRBLinExpr(); obj.AddTerm(2, tslref); obj.AddTerm(1, tsltc);
            //m.SetObjective(obj, GRB.MAXIMIZE);


            // — Single pass: enforce fairness and maximize 2·tslref + tsltc —
            m.AddConstr(E, GRB.GREATER_EQUAL, 0.7, "zz4");
            var obj = new GRBLinExpr();
            obj.AddTerm(2, tslref);
            obj.AddTerm(1, tsltc);
            m.SetObjective(obj, GRB.MAXIMIZE);
            m.Optimize();

            // write solution to file
            string p1File = Path.Combine(OutputDir, "phase1_result.txt");
            using (var sw = new StreamWriter(p1File, false))
            {
                sw.WriteLine($"Objective value: {m.ObjVal}");
                sw.WriteLine($"Total time taken: {m.Runtime:F6} seconds");

                // MIP gap is defined only if we stopped before proven optimal
                if (m.Status == GRB.Status.OPTIMAL)
                    sw.WriteLine("Optimal solution found. No MIP gap.");
                else
                    sw.WriteLine($"MIP gap: {m.MIPGap}");

                foreach (var variable in m.GetVars())
                    if (Math.Abs(variable.X) > 1e-6)            // print only non‑zeros
                        sw.WriteLine($"{variable.VarName} = {variable.X}");
            }
            Console.WriteLine($"Phase‑1 solution written → {p1File}\n");

            double phase1Gap = m.Status == GRB.Status.OPTIMAL ? 0.0 : m.MIPGap;
            Console.WriteLine($"Phase‑1 finished in {m.Runtime:F2} s  –  MIP gap = {phase1Gap:P4}");


            // ---------------- EXTRACT solution for later use -----------------
            var Wval = new double[P1_I, P1_T, P1_S];
            var Aval = new double[P1_L, P1_T];
            if (m.SolCount > 0)
            {
                for (int i = 0; i < P1_I; i++)
                    for (int t = 0; t < P1_T; t++)
                        for (int s = 0; s < P1_S; s++) Wval[i, t, s] = W[i, t, s].X;
                for (int l = 0; l < P1_L; l++)
                    for (int t = 0; t < P1_T; t++) Aval[l, t] = A[l, t].X;
            }
            /*
            if (m.SolCount == 0)                      // add this block
            {
                Console.WriteLine($"Phase-1 finished with status {m.Status} – no solution");
                m.Write("phase1.lp");                 // writes a diagnostic file
                Environment.Exit(1);                  // or throw/return null
            }*/
            Console.WriteLine("\n--- Phase 1 Decision Variables ---\n");

            // Print W[i,t,s]
            Console.WriteLine("W[i,t,s] values:");
            for (int i = 0; i < P1_I; i++)
                for (int t = 0; t < P1_T; t++)
                    for (int s = 0; s < P1_S; s++)
                        if (W[i, t, s].X > 1e-6) // only print non-zero values
                            Console.WriteLine($"W_{i}_{t}_{s} = {W[i, t, s].X}");

            // Print A[l,t]
            Console.WriteLine("\nA[l,t] values:");
            for (int l = 0; l < P1_L; l++)
                for (int t = 0; t < P1_T; t++)
                    if (A[l, t].X > 1e-6)
                        Console.WriteLine($"A_{l}_{t} = {A[l, t].X}");

            return new Phase1Result(m.ObjVal, Wval, Aval);
        }


        private static void AddLateFraction(
            GRBModel m,
            GRBVar[,,] W,
            int[] alpha,
            int[,] v,        // your 2D v[i,t] matrix
            int[,] n,
            double[] p,
            int type,
            double rhs
        )
        {
            // 1) compute denom exactly as in your standalone code
            double denom = 0.0;
            for (int i = 0; i < P1_I; i++)
            {
                if (alpha[i] != type) continue;
                // find the max over t
                double vmax_i = 0.0;
                for (int t = 0; t < P1_T; t++)
                    if (v[i, t] > vmax_i) vmax_i = v[i, t];

                for (int s = 0; s < P1_S; s++)
                    denom += n[i, s] * p[s] * vmax_i;
            }

            // 2) build the “late fraction” left-hand side
            var expr = new GRBLinExpr();
            for (int i = 0; i < P1_I; i++)
            {
                if (alpha[i] != type) continue;
                for (int t = 0; t < P1_T; t++)
                {
                    for (int s = 0; s < P1_S; s++)
                    {
                        expr.AddTerm(v[i, t] / denom, W[i, t, s]);
                    }
                }
            }

            m.AddConstr(expr, GRB.LESS_EQUAL, rhs, $"late_type{type}");
        }



        private static void AddTSL(GRBModel m, GRBVar[,,] W, int[] alpha, int[,] n, double[] p,
                                   int type, GRBVar tslVar, string cname)
        {
            double denom = 0;
            for (int i = 0; i < P1_I; i++) if (alpha[i] == type)
                    for (int s = 0; s < P1_S; s++) denom += n[i, s] * p[s];
            var expr = new GRBLinExpr();
            for (int i = 0; i < P1_I; i++) if (alpha[i] == type)
                    for (int s = 0; s < P1_S; s++)
                        for (int t = 0; t < P1_T; t++) expr.AddTerm(p[s] / denom, W[i, t, s]);
            expr.AddTerm(-1, tslVar);
            m.AddConstr(expr, GRB.EQUAL, 0, cname);
        }

        private static void BuildWeekData(Phase1Result p1, int week, int[,] cMat, int[,] nMat,
                                          out List<int> groupIds,
                                          out int[,] cilWeek, out int[,] nisWeek,
                                          out int[] allowedLocs)
        {
            groupIds = new List<int>();
            for (int i = 0; i < P1_I; i++)
            {
                bool yes = false;
                for (int s = 0; s < P1_S && !yes; s++)
                    if (p1.Wval[i, week, s] > 0.5) yes = true;
                if (yes) groupIds.Add(i + 1);  
            }
            int G = groupIds.Count;
            cilWeek = new int[G, P1_L];
            nisWeek = new int[G, P1_S];
            for (int r = 0; r < G; r++)
            {
                int gi = groupIds[r] - 1;
                for (int l = 0; l < P1_L; l++) cilWeek[r, l] = cMat[gi, l];
                for (int s = 0; s < P1_S; s++)
                    nisWeek[r, s] = (int)p1.Wval[gi, week, s];
            }
            var aList = new List<int>();
            for (int l = 0; l < P1_L; l++) if (p1.Aval[l, week] > 0.5) aList.Add(l + 1);
            allowedLocs = aList.ToArray();

            // OPTIONAL: Save to files
            //string wPrefix = $"output/w{week + 1}_";
            //File.WriteAllLines(wPrefix + "nis_maxtsl.txt", MatrixToLines(nisWeek));
            //File.WriteAllLines(wPrefix + "cil_maxtsl.txt", MatrixToLines(cilWeek));

            string wTag = (week + 1).ToString(CultureInfo.InvariantCulture);

            WriteMatrixWithHeader(Path.Combine(OutputDir, $"nis_w{wTag}_maxtsl.txt"),
                                  nisWeek,  
                                  groupIds,   
                                  P1_S);     

            WriteMatrixWithHeader(Path.Combine(OutputDir, $"cil_w{wTag}_maxtsl.txt"),
                                  cilWeek,   
                                  groupIds,
                                  P1_L);           


            // PRINT allowed locations
            Console.WriteLine($"\nAllowed locations for week {week + 1}: {string.Join(", ", allowedLocs)}\n");



        }

        private static IEnumerable<string> MatrixToLines(int[,] mat)
        {
            int rows = mat.GetLength(0);
            int cols = mat.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                string[] row = new string[cols];
                for (int j = 0; j < cols; j++)
                    row[j] = mat[i, j].ToString(CultureInfo.InvariantCulture);
                yield return string.Join('\t', row);
            }
        }

        private static void WriteMatrixWithHeader(string path,
                                                  int[,] mat,
                                                  IList<int> rowIds,
                                                  int headerCols)
        {
            using var sw = new StreamWriter(path, false);
            sw.Write('\t');                                          // empty corner cell
            for (int c = 1; c <= headerCols; c++) sw.Write($"{c}\t");
            sw.WriteLine();

            int rows = mat.GetLength(0);
            int cols = mat.GetLength(1);
            for (int r = 0; r < rows; r++)
            {
                sw.Write($"{rowIds[r]}\t");                          // 1‑based group id
                for (int c = 0; c < cols; c++) sw.Write($"{mat[r, c]}\t");
                sw.WriteLine();
            }
        }


        //PHASE‑2  –  WEEKLY VEHICLE ROUTING

        private static void SolvePhase2(int week, List<int> grpIds,
                                        int[,] cil, int[,] nis, double[,] dMat,
                                        int[] allowed, string outFile)
        {
            int G = grpIds.Count;
            using var env = new GRBEnv();
            using var m = new GRBModel(env) { ModelName = $"P2_week{week + 1}" };

            // ---------------- DECISION VARIABLES -----------------------------
            var y = new GRBVar[P2_M, P2_J, P2_L, P2_T];
            for (int v = 0; v < P2_M; v++)
                for (int j = 0; j < P2_J; j++)
                    for (int l = 0; l < P2_L; l++)
                        for (int t = 0; t < P2_T; t++)
                            y[v, j, l, t] = m.AddVar(0, 1, 0, GRB.BINARY,
                                                    $"y_m{v}_j{j}_l{l}_t{t}");

            var W = new GRBVar[G, P2_M, P2_T, P2_S];
            for (int g = 0; g < G; g++)
                for (int v = 0; v < P2_M; v++)
                    for (int t = 0; t < P2_T; t++)
                        for (int s = 0; s < P2_S; s++)
                            W[g, v, t, s] = m.AddVar(0, 450, 0, GRB.INTEGER,
                                                      $"W_{g}_{v}_{t}_{s}");

            var MM = new GRBVar[P2_M, P2_T, P2_S];
            for (int v = 0; v < P2_M; v++)
                for (int t = 0; t < P2_T; t++)
                    for (int s = 0; s < P2_S; s++)
                        MM[v, t, s] = m.AddVar(0, DayBudgetMin, 0, GRB.CONTINUOUS,
                                               $"MM_{v}_{t}_{s}");

            var U = new GRBVar[P2_L, P2_M, P2_T];           // MTZ order vars
            for (int l = 0; l < P2_L; l++)
                for (int v = 0; v < P2_M; v++)
                    for (int t = 0; t < P2_T; t++)
                        U[l, v, t] = m.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS,
                                             $"U_{l}_{v}_{t}");

            var z3 = m.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "z3");

            // ---------------- OBJECTIVE  (maximise util == z3) ---------------
            var util = new GRBLinExpr();
            for (int g = 0; g < G; g++)
                for (int v = 0; v < P2_M; v++)
                    for (int t = 0; t < P2_T; t++)
                        for (int s = 0; s < P2_S; s++) util.AddTerm(Prob[s], W[g, v, t, s]);

            var z3eq = new GRBLinExpr();
            z3eq.AddTerm(1.0, z3);          //  +1 · z3
            z3eq.MultAdd(-1.0, util);       //  -1 · util
            m.AddConstr(z3eq, GRB.EQUAL, 0, "z3_def");

            // objective:  maximise z3
            var obj = new GRBLinExpr();
            obj.AddTerm(1.0, z3);
            m.SetObjective(obj, GRB.MAXIMIZE);


            // ---------------- CONSTRAINTS ------------------------------------
            // Demand ≤ nis
            for (int g = 0; g < G; g++)
                for (int s = 0; s < P2_S; s++)
                {
                    var lhs = new GRBLinExpr();
                    for (int v = 0; v < P2_M; v++)
                        for (int t = 0; t < P2_T; t++) lhs.AddTerm(1, W[g, v, t, s]);
                    m.AddConstr(lhs, GRB.LESS_EQUAL, nis[g, s], $"dem_{g}_{s}");
                }

            // Leave & return depot each day
            for (int v = 0; v < P2_M; v++)
                for (int t = 0; t < P2_T; t++)
                {
                    var outDeg = new GRBLinExpr();
                    var inDeg = new GRBLinExpr();
                    for (int l = 0; l < P2_L; l++)
                    {
                        outDeg.AddTerm(1, y[v, 0, l, t]);
                        inDeg.AddTerm(1, y[v, l, 0, t]);
                    }
                    m.AddConstr(outDeg, GRB.EQUAL, 1, $"start_{v}_{t}");
                    m.AddConstr(inDeg, GRB.EQUAL, 1, $"end_{v}_{t}");
                }

            // Flow balance
            for (int v = 0; v < P2_M; v++)
                for (int l = 0; l < P2_L; l++)
                    for (int t = 0; t < P2_T; t++)
                    {
                        var fb = new GRBLinExpr();
                        for (int j = 0; j < P2_J; j++) fb.AddTerm(1, y[v, j, l, t]);
                        for (int j = 0; j < P2_J; j++) fb.AddTerm(-1, y[v, l, j, t]);
                        m.AddConstr(fb, GRB.EQUAL, 0, $"fb_{v}_{l}_{t}");
                    }

            // Must‑visit allowed locations
            foreach (int Lidx in allowed)
            {
                var vis = new GRBLinExpr();
                for (int v = 0; v < P2_M; v++)
                    for (int t = 0; t < P2_T; t++)
                        for (int j = 0; j < P2_L; j++) vis.AddTerm(1, y[v, j, Lidx - 1, t]);
                m.AddConstr(vis, GRB.GREATER_EQUAL, 1, $"must_{Lidx}");
            }

            // Daily time budget (travel + service + vaccinations) + slack MM
            for (int v = 0; v < P2_M; v++)
                for (int t = 0; t < P2_T; t++)
                    for (int s = 0; s < P2_S; s++)
                    {
                        var lhs = new GRBLinExpr();
                        for (int j = 0; j < P2_J; j++)
                            for (int l = 0; l < P2_L; l++)
                            {
                                lhs.Add(dMat[j, l] * y[v, j, l, t]);
                                if (l != 0) lhs.Add(StopServiceMin * y[v, j, l, t]);
                            }
                        for (int g = 0; g < G; g++) lhs.AddTerm(1, W[g, v, t, s]);
                        lhs.AddTerm(-1, MM[v, t, s]);
                        m.AddConstr(lhs, GRB.EQUAL, 0, $"time_{v}_{t}_{s}");
                        m.AddConstr(MM[v, t, s], GRB.LESS_EQUAL, DayBudgetMin, $"bud_{v}_{t}_{s}");
                    }

            // Coverage link
            for (int g = 0; g < G; g++)
                for (int v = 0; v < P2_M; v++)
                    for (int t = 0; t < P2_T; t++)
                        for (int s = 0; s < P2_S; s++)
                        {
                            var rhs = new GRBLinExpr();
                            for (int j = 0; j < P2_J; j++)
                                for (int l = 0; l < P2_L; l++) rhs.AddTerm(nis[g, s] * cil[g, l], y[v, j, l, t]);
                            m.AddConstr(W[g, v, t, s], GRB.LESS_EQUAL, rhs, $"cov_{g}_{v}_{t}_{s}");
                        }

            // MTZ subtour elimination
            for (int v = 0; v < P2_M; v++)
                for (int t = 0; t < P2_T; t++)
                    for (int j = 0; j < P2_L; j++)
                        for (int l = 1; l < P2_L; l++)
                        {
                            var lhs = new GRBLinExpr();
                            lhs.AddTerm(1, U[j, v, t]);
                            lhs.AddTerm(-1, U[l, v, t]);
                            lhs.AddTerm(P2_r + 1, y[v, j, l, t]);
                            m.AddConstr(lhs, GRB.LESS_EQUAL, P2_r, $"mtz_{v}_{j}_{l}_{t}");
                        }

            m.Parameters.NoRelHeurTime = 120;
            m.Parameters.TimeLimit = P2_TimeLimit;
            m.Parameters.MIPGap = 0.01;     //stop at <=1 % relative gap

            var watch = Stopwatch.StartNew();
            m.Optimize();
            watch.Stop();

            using var sw = new StreamWriter(outFile, false);
            if (m.SolCount > 0)
            {
                sw.WriteLine($"Objective value: {m.ObjVal}");
                sw.WriteLine($"Total time taken: {watch.Elapsed.TotalSeconds:F6} seconds");
                sw.WriteLine($"MIP gap: {m.MIPGap}");
                foreach (var v in m.GetVars())
                    if (Math.Abs(v.X) > 1e-6) sw.WriteLine($"{v.VarName} = {v.X}");
            }
            Console.WriteLine($"      ✓ week {week + 1} solved → {outFile}");
        }

        private static HashSet<(int m, int j, int l, int t)> BuildFixedArcs()
        {
            var arcs = new HashSet<(int, int, int, int)>();
            // regex:  y_m0_j19_l0_t2 = 1
            var rx = new Regex(@"^y_m(\d+)_j(\d+)_l(\d+)_t(\d+)\s*=\s*1$",
                               RegexOptions.Compiled);

            for (int w = 0; w < P1_T; w++)    
            {
                string f = Path.Combine(OutputDir, $"week{w + 1}.txt");
                if (!File.Exists(f)) continue;

                foreach (string line in File.ReadLines(f))
                {
                    var m = rx.Match(line.Trim());
                    if (!m.Success) continue;

                    int mId = int.Parse(m.Groups[1].Value);
                    int jId = int.Parse(m.Groups[2].Value);
                    int lId = int.Parse(m.Groups[3].Value);
                    int tLocal = int.Parse(m.Groups[4].Value);

                    int globalT = w * P2_T + tLocal; 
                    arcs.Add((mId, jId, lId, globalT));
                }
            }
            return arcs;
        }

        // PHASE - 3  

        private static void SolvePhase3(
                int[,] qMat,          // 3038 × 84   – instantaneous rate   q(i,t)
                int[,] cMat,          // 3038 × 21   – capacity of site l   c(i,l)
                double[,] dMat,       //   21 × 21   – travel time d(j,l)
                int[,] vMat,          // 3038 × 84   – value-per-shot       v(i,t)
                int[,] nMat)          // 3038 ×  3   – scenario demand      n(i,s)
        {

            const int M = P3_M, L = P3_L, T = P3_T, J = P3_J, S = P3_S, I = P3_I, K = P3_K;
            const int SETUP_MIN = P3_StopMin;        
            const int DAY_LIMIT = P3_DayLimit;      
            const int THETA = P3_theta;             
            double[] p = { 1.0 / 3, 1.0 / 3, 1.0 / 3 };
            int[] v2 = { 500, 500 };  
            int[] alpha = BuildAlpha();        // 1 for i < 68, else 2

            using var env = new GRBEnv();
            using var model = new GRBModel(env) { ModelName = "Phase-3" };

            var y = new GRBVar[M, J, L, T];
            var W = new GRBVar[I, M, T, S];
            var U = new GRBVar[J, M, T]; 
            var UU = new GRBVar[I];
            var B = new GRBVar[I, K];

            /* -------- unused MM[m] binary ------------- */
            GRBVar[] MM = new GRBVar[P3_I];
            for (int m = 0; m < M; m++)
                MM[m] = model.AddVar(0, 1, 0, GRB.BINARY, $"MM_{m}");


            /* -------- routing y[m,j,l,t] ------------------------ */
            for (int m = 0; m < M; m++)
                for (int j = 0; j < J; j++)
                    for (int l = 0; l < L; l++)
                        for (int t = 0; t < T; t++)
                            y[m, j, l, t] = model.AddVar(0, 1, 0, GRB.BINARY,
                                                         $"y_{m}_{j}_{l}_{t}");

            /* -------- vaccine flow W[i,m,t,s] ------------------- */
            for (int i = 0; i < I; i++)
                for (int m = 0; m < M; m++)
                    for (int t = 0; t < T; t++)
                        for (int s = 0; s < S; s++)
                            W[i, m, t, s] = model.AddVar(0, 450, 0, GRB.INTEGER,
                                                         $"W_{i}_{m}_{t}_{s}");

            /* -------- MTZ helper U[j,m,t] ----------------------- */
            for (int j = 0; j < J; j++)
                for (int m = 0; m < M; m++)
                    for (int t = 0; t < T; t++)
                        U[j, m, t] = model.AddVar(0, GRB.INFINITY, 0,
                                                  GRB.CONTINUOUS, $"U_{j}_{m}_{t}");

            /* -------- utility UU[i] ----------------------------- */
            for (int i = 0; i < I; i++)
                UU[i] = model.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, $"UU_{i}");

            /* -------- pair-wise gini vars ----- */
            for (int i = 0; i < I; i++)
                for (int k = 0; k < K; k++)
                    B[i, k] = model.AddVar(0, GRB.INFINITY, 0,
                                           GRB.CONTINUOUS, $"B_{i}_{k}");

            GRBVar z1 = model.AddVar(-GRB.INFINITY, GRB.INFINITY, 0, GRB.CONTINUOUS, "z1");
            GRBVar z2 = model.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "z2");
            GRBVar z3 = model.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "z3");
            GRBVar z4 = model.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "z4");
            GRBVar E = model.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "E");
            GRBVar toplam = model.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "toplam");
            GRBVar tslref = model.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "tslref");
            GRBVar tsltc = model.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "tsltc");

            // ---------- D-Index auxiliary variables ----------------------------------
            GRBVar[] F = new GRBVar[I];              // |Uavg – Ui|
            for (int i = 0; i < I; i++)
                F[i] = model.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, $"F_{i}");

            GRBVar Uavg = model.AddVar(0, GRB.INFINITY, 0, GRB.CONTINUOUS, "Uavg");

            // ---------- εi (population shares)  --------------------------------------
            double[] eps = new double[I];
            double totPop = 0;
            for (int i = 0; i < I; i++)
                for (int s = 0; s < S; s++) totPop += nMat[i, s] * p[s];

            for (int i = 0; i < I; i++)
            {
                double sum = 0;
                for (int s = 0; s < S; s++) sum += nMat[i, s] * p[s];
                eps[i] = sum / totPop;
            }

            //  Constraints 

            /* --- 4.1 daily time budget -------------------------------------- */
            for (int m = 0; m < M; m++)
                for (int t = 0; t < T; t++)
                    for (int s = 0; s < S; s++)
                    {
                        var expr = new GRBLinExpr();
                        for (int j = 0; j < J; j++)
                            for (int l = 0; l < L; l++)
                            {
                                expr.AddTerm(dMat[j, l], y[m, j, l, t]);
                                if (l > 0) expr.AddTerm(SETUP_MIN, y[m, j, l, t]);
                            }
                        for (int i = 0; i < I; i++)
                            expr.AddTerm(1, W[i, m, t, s]);

                        model.AddConstr(expr, GRB.LESS_EQUAL, DAY_LIMIT,
                                        $"capacity_{m}_{t}_{s}");
                    }

            /* --- 4.2 location capacity c(i,l) ------------------------------- */
            for (int i = 0; i < I; i++)
                for (int m = 0; m < M; m++)
                    for (int t = 0; t < T; t++)
                        for (int s = 0; s < S; s++)
                        {
                            var lhs = new GRBLinExpr();
                            lhs.AddTerm(1.0, W[i, m, t, s]);
                            for (int j = 0; j < J; j++)
                                for (int l = 0; l < L; l++)
                                    lhs.AddTerm(-nMat[i, s] * cMat[i, l],
                                                y[m, j, l, t]);

                            model.AddConstr(lhs, GRB.LESS_EQUAL, 0,
                                            $"siteCap_{i}_{m}_{t}_{s}");
                        }

            /* --- 4.3 scenario demand cap ----------------------------------- */
            for (int i = 0; i < I; i++)
                for (int s = 0; s < S; s++)
                {
                    var lhs = new GRBLinExpr();
                    for (int m = 0; m < M; m++)
                        for (int t = 0; t < T; t++)
                            lhs.AddTerm(1, W[i, m, t, s]);
                    model.AddConstr(lhs, GRB.LESS_EQUAL, nMat[i, s],
                                    $"demand_{i}_{s}");
                }

            /* --- 4.4 instantaneous rate cap q(i,t) -------------------------- */
            for (int i = 0; i < I; i++)
                for (int m = 0; m < M; m++)
                    for (int t = 0; t < T; t++)
                        for (int s = 0; s < S; s++)
                            model.AddConstr(W[i, m, t, s],
                                            GRB.LESS_EQUAL,
                                            nMat[i, s] * qMat[i, t],
                                            $"rate_{i}_{m}_{t}_{s}");

            /* --- 4.5 utility balance ---------------------------------------- */
            for (int i = 0; i < I; i++)
            {
                var bal = new GRBLinExpr();
                for (int s = 0; s < S; s++)
                    bal.AddTerm(THETA * nMat[i, s], UU[i]);
                for (int m = 0; m < M; m++)
                    for (int t = 0; t < T; t++)
                        for (int s = 0; s < S; s++)
                            bal.AddTerm(-THETA, W[i, m, t, s]);
                for (int m = 0; m < M; m++)
                    for (int t = 0; t < T; t++)
                        for (int s = 0; s < S; s++)
                            bal.AddTerm(vMat[i, t], W[i, m, t, s]);

                model.AddConstr(bal, GRB.EQUAL, 0, $"utility_{i}");
            }

            /* --- 4.6 floor UU(i) ≥ 0.7 -------------------------------------- */
            for (int i = 0; i < I; i++)
                model.AddConstr(UU[i], GRB.GREATER_EQUAL, 0.7, $"minUU_{i}");

            // ---------- D-Index linearisation  ---------------------------------------
            // 1. Uavg = Σ εi · Ui
            var avgEq3 = new GRBLinExpr();
            for (int i = 0; i < I; i++) avgEq3.AddTerm(eps[i], UU[i]);
            avgEq3.AddTerm(-1, Uavg);
            model.AddConstr(avgEq3, GRB.EQUAL, 0, "Uavg_def");

            // 2. |Ui – Uavg|  →  Fi
            for (int i = 0; i < I; i++)
            {
                var c1 = new GRBLinExpr();      // Ui – Uavg ≤ Fi
                c1.AddTerm(1, UU[i]); c1.AddTerm(-1, Uavg); c1.AddTerm(-1, F[i]);
                model.AddConstr(c1, GRB.LESS_EQUAL, 0, $"abs1_{i}");

                var c2 = new GRBLinExpr();      // Uavg – Ui ≤ Fi
                c2.AddTerm(1, Uavg); c2.AddTerm(-1, UU[i]); c2.AddTerm(-1, F[i]);
                model.AddConstr(c2, GRB.LESS_EQUAL, 0, $"abs2_{i}");
            }

            // 3. Σ εi Fi ≤ 2 · D_BOUND · Uavg
            var dLhs3 = new GRBLinExpr();
            for (int i = 0; i < I; i++) dLhs3.AddTerm(eps[i], F[i]);
            var dRhs3 = new GRBLinExpr(); dRhs3.AddTerm(2 * D_BOUND, Uavg);
            model.AddConstr(dLhs3, GRB.LESS_EQUAL, dRhs3, "Dindex");


            /* --- 4.7 two tier service level equations ----------------------- */
            double denomRef = 0, denomTC = 0;
            for (int i = 0; i < I; i++)
                for (int s = 0; s < S; s++)
                    if (alpha[i] == 2) denomRef += nMat[i, s] * p[s];
                    else denomTC += nMat[i, s] * p[s];

            var refEq = new GRBLinExpr();
            var tcEq = new GRBLinExpr();
            for (int i = 0; i < I; i++)
                for (int s = 0; s < S; s++)
                    for (int t = 0; t < T; t++)
                        for (int m = 0; m < M; m++)
                        {
                            if (alpha[i] == 2)
                                refEq.AddTerm(p[s] / denomRef, W[i, m, t, s]);
                            else
                                tcEq.AddTerm(p[s] / denomTC, W[i, m, t, s]);
                        }
            refEq.AddTerm(-1, tslref);
            tcEq.AddTerm(-1, tsltc);
            model.AddConstr(refEq, GRB.EQUAL, 0, "z2_ref");
            model.AddConstr(tcEq, GRB.EQUAL, 0, "z2_tc");

            /* --- 4.8 late-service caps -------------------------------------- */
            double[] vmax = new double[I];
            for (int i = 0; i < I; i++)
                for (int t = 0; t < T; t++)
                    vmax[i] = Math.Max(vmax[i], vMat[i, t]);

            double denomLate1 = 0, denomLate2 = 0;
            for (int i = 0; i < I; i++)
                for (int s = 0; s < S; s++)
                    if (alpha[i] == 1) denomLate1 += nMat[i, s] * p[s] * vmax[i];
                    else denomLate2 += nMat[i, s] * p[s] * vmax[i];

            var late1 = new GRBLinExpr();
            var late2 = new GRBLinExpr();
            for (int i = 0; i < I; i++)
                for (int s = 0; s < S; s++)
                    for (int t = 0; t < T; t++)
                        for (int m = 0; m < M; m++)
                        {
                            if (alpha[i] == 1)
                                late1.AddTerm(vMat[i, t] / denomLate1, W[i, m, t, s]);
                            else
                                late2.AddTerm(vMat[i, t] / denomLate2, W[i, m, t, s]);
                        }
            model.AddConstr(late1, GRB.LESS_EQUAL, 0.2, "lateA1");
            model.AddConstr(late2, GRB.LESS_EQUAL, 0.1, "lateA2");

            /* -------------- optional Gini constraints --------
            for (int i = 0; i < I; i++)
                for (int k = 0; k < K; k++)
                {
                    var lhs1 = new GRBLinExpr(); lhs1.AddTerm(1, UU[i]); lhs1.AddTerm(-1, UU[k]); lhs1.AddTerm(-1, B[i,k]);
                    model.AddConstr(lhs1, GRB.LESS_EQUAL, 0, $"gini1_{i}_{k}");

                    var lhs2 = new GRBLinExpr(); lhs2.AddTerm(1, UU[k]); lhs2.AddTerm(-1, UU[i]); lhs2.AddTerm(-1, B[i,k]);
                    model.AddConstr(lhs2, GRB.LESS_EQUAL, 0, $"gini2_{i}_{k}");
                }

            var gSum = new GRBLinExpr();
            for (int i = 0; i < I; i++)
                for (int k = 0; k < K; k++) gSum.AddTerm(1, B[i,k]);
            for (int i = 0; i < I; i++) gSum.AddTerm(-27.8, UU[i]);
            model.AddConstr(gSum, GRB.LESS_EQUAL, 0, "gini3");
            --------------------------------------------------------------------------- */

            model.SetObjective(2 * tslref + tsltc, GRB.MAXIMIZE);

            // Dynamic hash set calculation
            HashSet<(int, int, int, int)> fixedArcs = BuildFixedArcs(); 

            for (int m = 0; m < M; m++)
                for (int j = 0; j < J; j++)
                    for (int l = 0; l < L; l++)
                        for (int t = 0; t < T; t++)
                            if (fixedArcs.Contains((m, j, l, t)))
                            {
                                y[m, j, l, t].LB = 1;
                                y[m, j, l, t].UB = 1;
                            }
                            else
                            {
                                y[m, j, l, t].LB = 0;
                                y[m, j, l, t].UB = 0;
                            }

            // Optimization
            model.Parameters.NoRelHeurTime = 60;
            model.Parameters.TimeLimit = P3_TimeLimit; 
            model.Optimize();

            if (model.Status == GRB.Status.INF_OR_UNBD)
            {
                model.ComputeIIS();
                model.Write(Path.Combine(OutputDir, "phase3.ilp"));
                Console.WriteLine("Phase-3 infeasible – IIS written.");
                return;
            }

            string outFile = Path.Combine(OutputDir, "phase3_result.txt");
            using var wr = new StreamWriter(outFile);
            wr.WriteLine($"Objective value : {model.ObjVal}");
            wr.WriteLine($"MIP gap         : {model.Get(GRB.DoubleAttr.MIPGap):F4}");
            foreach (GRBVar v in model.GetVars())
                if (Math.Abs(v.X) > 1e-6) wr.WriteLine($"{v.VarName} = {v.X}");
            Console.WriteLine($"Phase-3 solved – see {outFile}");
        }

        private static int[] BuildAlpha()
        {
            int[] a = new int[P3_I];
            for (int i = 0; i < 68; i++) a[i] = 1;
            for (int i = 68; i < P3_I; i++) a[i] = 2;
            return a;
        }
    }
}