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
                            obj += d[j, l] * y2[m, j, l, t];
                        }
                    }
                    for (int l = 0; l < L; l++)
                    {
                        for (int t = 0; t < T; t++)
                        {
                            obj += d[l, j] * y1[m, l, j, t];
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






            // Optimize model
            model.Optimize();

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
