using Gurobi;
using System;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            GRBEnv env = new GRBEnv();
            GRBModel model = new GRBModel(env);

            int M = 2; //num of mobile facilities
            int L = 9; //num of stopping locations
            int T = 12; //num of days vaccine will take place
            int J = 9; //num of origin loc. for mob. fac.
            int S = 10; //num of scenarios tm
            int I = 10; //num of groups
            int r = 5;

            int[,] q = new int[62, 12] {
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 1,1,1,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1},
                { 0,0,0,1,1,1,1,1,1,1,1,1 },
                { 0,0,0,1,1,1,1,1,1,1,1,1 },
                { 0,0,0,1,1,1,1,1,1,1,1,1 },
                { 0,0,0,1,1,1,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,1,1,1,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 },
                { 0,0,0,0,0,0,0,0,0,1,1,1 } };






            int[,] c = new int[62, 8] {
            {1, 0, 0, 0, 0, 0, 0, 0},
            {0, 1, 0, 0, 0, 0, 0, 0},
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {0, 0, 0, 0, 1, 0, 0, 0},
            {0, 0, 0, 0, 0, 1, 0, 0},
            {0, 0, 0, 0, 0, 0, 1, 0},
            {0, 0, 0, 0, 0, 0, 0, 1},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {0, 1, 0, 0, 0, 0, 0, 0},
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {0, 0, 0, 0, 1, 0, 0, 0},
            {0, 0, 0, 0, 0, 1, 0, 0},
            {0, 0, 0, 0, 0, 0, 1, 0},
            {0, 0, 0, 0, 0, 0, 0, 1},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {0, 1, 0, 0, 0, 0, 0, 0},
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {0, 0, 0, 0, 0, 1, 0, 0},
            {0, 0, 0, 0, 0, 0, 1, 0},
            {0, 0, 0, 0, 0, 0, 0, 1},
            {1, 0, 0, 0, 0, 0, 0, 0},
            {0, 1, 0, 0, 0, 0, 0, 0},
            {0, 0, 1, 0, 0, 0, 0, 0},
            {0, 0, 0, 1, 0, 0, 0, 0},
            {0, 0, 0, 0, 1, 0, 0, 0},
            {0, 0, 0, 0, 0, 1, 0, 0},
            {0, 0, 0, 0, 0, 0, 1, 0},
            {0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 0, 0},
            { 0, 1, 0, 0, 0, 0, 0, 0},
            { 0, 0, 1, 0, 0, 0, 0, 0},
            { 0, 0, 0, 1, 0, 0, 0, 0},
            { 0, 0, 0, 0, 0, 1, 0, 0},
            { 0, 0, 0, 0, 0, 0, 1, 0},
            { 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 0, 0},
            { 0, 1, 0, 0, 0, 0, 0, 0},
            { 0, 0, 1, 0, 0, 0, 0, 0},
            { 0, 0, 0, 1, 0, 0, 0, 0},
            { 0, 0, 0, 0, 1, 0, 0, 0},
            { 0, 0, 0, 0, 0, 1, 0, 0},
            { 0, 0, 0, 0, 0, 0, 1, 0},
            { 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 0, 0},
            { 0, 1, 0, 0, 0, 0, 0, 0},
            { 0, 0, 1, 0, 0, 0, 0, 0},
            { 0, 0, 0, 1, 0, 0, 0, 0},
            { 0, 0, 0, 0, 1, 0, 0, 0},
            { 0, 0, 0, 0, 0, 1, 0, 0},
            { 0, 0, 0, 0, 0, 0, 1, 0},
            { 0, 0, 0, 0, 0, 0, 0, 1},
            { 1, 0, 0, 0, 0, 0, 0, 0},
            { 0, 1, 0, 0, 0, 0, 0, 0},
            { 0, 0, 1, 0, 0, 0, 0, 0},
            { 0, 0, 0, 1, 0, 0, 0, 0},
            { 0, 0, 0, 0, 1, 0, 0, 0},
            { 0, 0, 0, 0, 0, 1, 0, 0},
            { 0, 0, 0, 0, 0, 0, 1, 0},
            { 0, 0, 0, 0, 0, 0, 0, 1}};




    // Define the values of d[j, l]
    double[,] d = new double[,]
            {
                { 0, 8.8, 23.3, 39.6, 8, 35.3, 4.6, 12.4, 31.1 },
                { 7.8, 0, 32.7, 30.5, 11.5, 43.3, 5.1, 21, 31.2 },
                { 23.1, 28.3, 0, 36.3, 17.5, 16.4, 30.8, 24.3, 12.7 },
                { 38.6, 30.4, 36.9, 0, 29.1, 38.8, 29.7, 47, 21.8 },
                { 7.7, 11.4, 17.6, 29.2, 0, 28.2, 5.8, 16.9, 20.7 },
                { 38.2, 42.1, 16.4, 38.6, 28.1, 0, 41.4, 39.8, 17.9 },
                { 3.3, 5.3, 22.7, 34.7, 6.4, 33.3, 0, 15.4, 25.6 },
                { 12.7, 21.4, 24.5, 46.1, 17.7, 40.4, 15.8, 0, 32.7 },
                { 30.5, 32.1, 12.7, 21.8, 20.4, 17.9, 33.6, 32.1, 0 }
            };

            
            int[] v = new int[] { 40, 50 };

            int[,] n = new int[,] {
                { 1120, 1401, 1681 },
                { 8, 11, 13 },
                { 8, 10, 11 },
                { 1528, 1910, 2292 },
                { 1, 1, 1 },
                { 4194, 5242, 6290 },
                { 56, 70, 84 },
                { 6, 7, 8 },
                { 179, 214, 250 },
                { 143, 171, 200 },
                { 119, 143, 167 },
                { 9, 10, 12 },
                { 75, 89, 104 },
                { 186, 223, 260 },
                { 143, 171, 200 },
                { 147, 176, 206 },
                { 1115, 1394, 1673 },
                { 11, 14, 17 },
                { 7, 9, 10 },
                { 1472, 1840, 2207 },
                { 4220, 5276, 6331 },
                { 75, 94, 113 },
                { 6, 7, 8 },
                { 210, 251, 293 },
                { 154, 184, 215 },
                { 113, 136, 158 },
                { 7, 8, 9 },
                { 88, 105, 123 },
                { 174, 208, 243 },
                { 168, 201, 235 },
                { 143, 172, 200 },
                { 1086, 1358, 1630 },
                { 13, 17, 20 },
                { 6, 8, 9 },
                { 1487, 1859, 2230 },
                { 4200, 5250, 6300 },
                { 59, 74, 89 },
                { 6, 8, 9 },
                { 166, 199, 232 },
                { 137, 164, 191 },
                { 121, 145, 169 },
                { 7, 8, 10 },
                { 71, 85, 99 },
                { 174, 209, 244 },
                { 157, 188, 220 },
                { 146, 175, 204 },
                { 1163, 1454, 1745 },
                { 11, 14, 17 },
                { 6, 7, 8 },
                { 1509, 1886, 2263 },
                { 1, 1, 1 },
                { 4252, 5315, 6377 },
                { 57, 72, 86 },
                { 4, 6, 7 },
                { 188, 225, 263 },
                { 123, 147, 172 },
                { 123, 148, 172 },
                { 5, 6, 7 },
                { 78, 93, 109 },
                { 201, 241, 281 },
                { 171, 205, 239 },
                { 131, 157, 183 }
            };


            // Create a 2D list d[j, l]
            /*
            List<List<double>> d = new List<List<double>>();

            for (int j = 0; j < J; j++)
            {
                d.Add(new List<double>());

                for (int l = 0; l < L; l++)
                {
                    d[j].Add(dValues[j, l]);
                }
            }*/


            // Define nodes


            int[] set_M = new int[M];
            for (int i = 0; i < M; i++)
            {
                set_M[i] = i;
            }

            int[] set_L = new int[L];
            for (int i = 0; i < L; i++)
            {
                set_L[i] = i;
            }

            int[] set_T = new int[T];
            for (int i = 0; i < T; i++)
            {
                set_T[i] = i;
            }

            int[] set_J = new int[J];
            for (int i = 0; i < J; i++)
            {
                set_J[i] = i;
            }

            int[] set_S = new int[S];
            for (int i = 0; i < S; i++)
            {
                set_S[i] = i;
            }

            int[] set_I = new int[I];
            for (int i = 0; i < I; i++)
            {
                set_I[i] = i;
            }

            // Define arcs
            /*
            int numArcs = numNodes * (numNodes - 1);
            int[] from = new int[numArcs];
            int[] to = new int[numArcs];
            double[] distance = new double[numArcs];
            int k = 0;
            for (int i = 0; i < numNodes; i++)
            {
                for (int j = 0; j < numNodes; j++)
                {
                    if (i != j)
                    {
                        from[k] = i;
                        to[k] = j;
                        distance[k] = Math.Sqrt(Math.Pow(i - j, 2) + Math.Pow(i - j, 2));
                        k++;
                    }
                }
            }
            */


            // Define variables
            /*
            GRBVar[] x = new GRBVar[numArcs];
            for (int i = 0; i < numArcs; i++)
            {
                x[i] = model.AddVar(0.0, 1.0, distance[i], GRB.BINARY, "x" + i.ToString());
            }*/


            // Create 4D decision variable y[m, j, l, t]
            GRBVar[,,,] y1 = new GRBVar[M, J, L, T];

            for (int m = 0; m < M; m++)
            {
                for (int j = 0; j < J; j++)
                {
                    for (int l = 0; l < L; l++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            y1[m, j, l, t] = model.AddVar(0, 1, 0, GRB.BINARY, $"y1_m{m}_j{j}_l{l}_t{t}");
                        }
                    }
                }
            }

            GRBVar[,,,] y2 = new GRBVar[M, L, J, T];

            for (int m = 0; m < M; m++)
            {
                for (int j = 0; j < J; j++)
                {
                    for (int l = 0; l < L; l++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            y2[m, l, j, t] = model.AddVar(0, 1, 0, GRB.BINARY, $"y2_m{m}_l{l}_j{j}_t{t}");
                        }
                    }
                }
            }

            GRBVar[,,,] W = new GRBVar[I, M, T, S];

            for (int i = 0; i < I; i++)
            {
                for (int m = 0; m < M; m++)
                {
                    for (int t = 0; t < T; t++)
                    {
                        for (int s = 0; s < S; s++)
                        {
                            W[i, m, t, s] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.INTEGER, "W_" + i + "_" + m + "_" + t + "_" + s);
                        }
                    }
                }
            }

            GRBVar[,,] U = new GRBVar[J, M, T];

            for (int j = 0; j < J; j++)
            {
                for (int m = 0; m < M; m++)
                {
                    for (int t = 0; t < T; t++)
                    {
                       
                            U[j, m, t] = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.INTEGER, "U_" + j + "_" + m + "_" + t);
                        }
                    }
                }
            


            /*

            // Define objective function
            GRBLinExpr expr = new GRBLinExpr();
            for (int i = 0; i < numArcs; i++)
            {
                expr.AddTerm(distance[i], x[i]);
            }
            model.SetObjective(expr, GRB.MINIMIZE);
            */

            GRBLinExpr obj = 0;
            for (int m = 0; m < M; m++)
            {
                for (int j = 0; j < J; j++)
                {
                    for (int l = 0; l < L; l++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            obj += d[j, l] * y1[m, j, l, t];
                        }
                    }
                    for (int l = 0; l < L; l++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            obj += d[l, j] * y2[m, l, j, t];
                        }
                    }
                }
            }
            model.SetObjective(obj, GRB.MINIMIZE);




            // Define constraints
            /*
            for (int i = 0; i < numNodes; i++)
            {
                GRBLinExpr expr1 = new GRBLinExpr();
                for (int j = 0; j < numArcs; j++)
                {
                    if (from[j] == i)
                    {
                        expr1.AddTerm(1.0, x[j]);
                    }
                }
                model.AddConstr(expr1, GRB.EQUAL, 1.0, "out_" + i.ToString());

                GRBLinExpr expr2 = new GRBLinExpr();
                for (int j = 0; j < numArcs; j++)
                {
                    if (to[j] == i)
                    {
                        expr2.AddTerm(1.0, x[j]);
                    }
                }
                model.AddConstr(expr2, GRB.EQUAL, 1.0, "in_" + i.ToString());
            }*/

            for (int m = 0; m < M; m++)
            {
                for (int j = 0; j < J; j++)
                {
                    for (int t = 0; t < T; t++)
                    {
                        GRBLinExpr expr = 0;
                        for (int l = 0; l < L; l++)
                        {
                            expr += y2[m, l, j, t];
                        }
                        model.AddConstr(expr == 1, $"cons1_m{m}_j{j}_t{t}");
                    }
                }
            }

            for (int m = 0; m < M; m++)
            {
                for (int j = 0; j < J; j++)
                {
                    for (int t = 0; t < T; t++)
                    {
                        GRBLinExpr expr = 0;
                        for (int l = 0; l < L; l++)
                        {
                            expr += y1[m, j, l, t];
                        }
                        model.AddConstr(expr == 1, $"cons2_m{m}_j{j}_t{t}");
                    }
                }
            }

            for (int m = 0; m < M; m++)
            {
                for (int l = 0; l < L; l++)
                {
                    for (int t = 0; t < T; t++)
                    {
                        GRBLinExpr lhs = 0;
                        GRBLinExpr rhs = 0;

                        // Sum over j in J union L y[m, j, l, t]
                        for (int j = 0; j < J; j++)
                        {
                            lhs += y1[m, j, l, t];
                            lhs += y2[m, l, j, t]; // Sum over l in L
                        }

                        // Sum over j in J union L y[m, l, j, t]
                        for (int j = 0; j < J; j++)
                        {
                            rhs += y2[m, l, j, t];
                            rhs += y1[m, j, l, t]; // Sum over l in L
                        }

                        // Add constraint: sum over j in J union L y[m, j, l, t] - sum over j in J union L y[m, l, j, t] = 0
                        model.AddConstr(lhs - rhs == 0, $"flow_balance_m{m}_l{l}_t{t}");
                    }
                }
            }


            for (int m = 0; m < M; m++)
            {
                for (int t = 0; t < T; t++)
                {
                    for (int s = 0; s < S; s++)
                    {
                        GRBLinExpr lhs = 0;
                        for (int i = 0; i < I; i++)
                        {
                            lhs += W[i, m, t, s];
                        }
                        model.AddConstr(lhs <= v[m], $"cons5_m{m}_t{t}_s{s}");
                    }
                }
            }

            for (int i = 0; i < I; i++)
            {
                for (int m = 0; m < M; m++)
                {
                    for (int l = 0; l < L; l++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            for (int s = 0; s < S; s++)
                            {
                                GRBLinExpr lhs = W[i, m, t, s];
                                GRBLinExpr rhs = 0;

                                for (int j = 0; j < J; j++)
                                {
                                    rhs += y1[m, j, l, t];
                                }

                                for (int j = 0; j < L; j++)
                                {
                                    rhs += y2[m, l, j, t];
                                }

                                model.AddConstr(lhs <= n[i, s] * c[i, l] * rhs, $"cons6_i{i}_m{m}_l{l}_t{t}_s{s}");
                            }
                        }
                    }
                }
            }

            for (int s = 0; s < S; s++)
            {
                for (int i = 0; i < I; i++)
                {
                    GRBLinExpr lhs = 0;

                    // Sum over m in M, sum over t in T
                    for (int m = 0; m < M; m++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            lhs += W[i, m, t, s];
                        }
                    }

                    // Add constraint: lhs <= n[i, s]
                    model.AddConstr(lhs <= n[i, s], $"cons7_i{i}_s{s}");
                }
            }

            for (int m = 0; m < M; m++)
            {
                for (int t = 0; t < T; t++)
                {
                    for (int s = 0; s < S; s++)
                    {
                        for (int i = 0; i < I; i++)
                        {
                            GRBLinExpr lhs = W[i, m, t, s];
                            double rhs = n[i, s] * q[i, t];

                            // Add constraint: lhs <= rhs
                            model.AddConstr(lhs <= rhs, $"cons8_m{m}_t{t}_s{s}_i{i}");
                        }
                    }
                }
            }


            for (int m = 0; m < M; m++)
            {
                for (int t = 0; t < T; t++)
                {
                    for (int j = 0; j < L; j++)
                    {
                        for (int l = 0; l < L; l++)
                        {
                            if (j != l) // Avoid double counting for j=l
                            {
                                GRBLinExpr lhs = U[m, j, t] - U[m, l, t] + (r + 1) * y1[m, j, l, t];
                                model.AddConstr(lhs <= r, $"cons9_m{m}_j{j}_l{l}_t{t}");
                            }
                        }
                    }
                }
            }
            //double countingden emin değilim




            // Optimize model
            model.Optimize();
            model.Write("model.lp");

            // Print solution
            Console.WriteLine("Objective value: " + model.ObjVal);
            /*
            for (int i = 0; i < numArcs; i++)
            {
                if (x[i].X > 0.5)
                {
                    Console.WriteLine("x[" + from[i].ToString() + "," + to[i].ToString() + "] = " + x[i].X);
                }
            }
            */
            // Dispose of model and environment
            model.Dispose();
            env.Dispose();
        }
        catch (GRBException e)
        {
            Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
        }
    }
}
