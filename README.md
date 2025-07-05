# VaccineRouting — Full Pipeline with Min-max, Gini and D-Index Equity

A three-phase Gurobi model for planning an **equitable mobile-vaccination campaign**.

| Phase | Purpose |
|-------|---------|
| **1. Weekly sizing** | Pick stop locations and allocate weekly dose totals |
| **2. Vehicle routing** | Build three-day routes for each week (2 vehicles) |
| **3. Integrated plan** | Produce one 12-day, two-vehicle schedule |

The model supports three alternative fairness constraints:

| Policy | Where to enable / disable in `Program.cs` |
|--------|-----------------------------------------------|
| **Min-max** (≥ 0.7) | line `m.AddConstr(E, GRB.GREATER_EQUAL, 0.7, …)` |
| **Gini** | uncomment the block labelled *gini* (3 constraints) |
| **D-Index** | block labelled *D-Index* (3 constraints, active by default) |

Comment or uncomment the blocks you need; they are independent.

---

## Folder layout

```
.
├── origindata/        # raw text data
│   ├── qit_20*.txt    # instant vaccination rate  q(i,t)
│   ├── cil_20.txt     # capacity if group i served at location l
│   ├── djl_20.txt     # travel minutes from origin j to stop l
│   ├── vit_20*.txt    # value per dose  v(i,t)
│   └── nis_20.txt     # scenario demand n(i,s)
├── output/            # created automatically – solutions land here
├── VaccineRouting.csproj
└── Program.cs         # the full three-phase pipeline
```

---

## Quick start

```bash
# build
dotnet build -c Release

# run (≈ 5 min per phase on a laptop)
dotnet run -c Release
```

Results are written to  

* `output/phase1_result.txt`  
* `output/week*.txt` (one per week, Phase 2)  
* `output/phase3_result.txt`

If a phase hits its time limit you will still get the incumbent solution and its MIP gap.

---

## Tunable parameters (edit **`Program.cs`**)

| Constant | Meaning | Default |
|----------|---------|---------|
| `D_BOUND` | Max allowed **D-Index** (0 = off, 1 = no cap) | `0.25` |
| `P1_TimeLimit`, `P2_TimeLimit`, `P3_TimeLimit` | solver time limits in seconds | `300` each |
| `StopServiceMin` | minutes spent at each stop | `30` |
| `DayBudgetMin`   | total minutes per vehicle per day | `480` |
| `Prob[]`         | scenario probabilities | `1/3, 1/3, 1/3` |
| `P1_theta`, `P3_theta` | utility weight in the objective | `6` |

Tip: lowering `m.Parameters.MIPGap` (Phase 2) or raising the time limits tightens optimality at the cost of runtime.

---

## Reading the output

| File / variable | Interpretation |
|-----------------|----------------|
| **Phase 1**<br>`W_i_t_s` | Doses for group *i* in week *t* (scenario *s*) |
| `A_l_t` | 1 if stop *l* chosen in week *t* |
| **Phase 2** (week *k*)<br>`y_mjlt` | 1 if vehicle *m* travels j → l on day *t* |
| `W_g_v_t_s` | Doses for reduced group index *g* by vehicle *v* on day *t* |
| **Phase 3** | Same names, now for the 12‑day horizon |

---

## Typical hiccups

| Symptom | Likely cause | Remedy |
|---------|--------------|--------|
| `Status = INF_OR_UNBD` | fairness too tight | relax `D_BOUND`, or comment extra equity blocks |
| First feasible solution slow | heuristics need more time | raise `NoRelHeurTime` (Phase 1 & 3) |
| Day‑budget violated | travel matrix in minutes? `DayBudgetMin` too small | double‑check `djl_20.txt` and the constant |

---

## Acknowledgements

Based on the multi‑objective mobile‑vaccination model developed at Koç University with Prof. Sibel Salman.  
Optimised with the **[Gurobi Optimizer](https://www.gurobi.com/)**.

Feel free to open issues or pull requests if you extend the model.
