
# VaccineRouting — Full Pipeline with Min‑max, Gini & D‑Index Equity  
*(vacstep4 project)*

A three‑phase Gurobi model that plans an **equitable mobile‑vaccination campaign**.

| Phase | Role |
|-------|------|
| **1 – Weekly sizing** | Pick stop locations & decide weekly dose totals |
| **2 – Vehicle routing** | Build 3‑day routes for each week (2 vehicles) |
| **3 – Integrated plan** | Produce one 12‑day, two‑vehicle schedule |

The model can enforce three independent equity rules:

| Policy | How to enable / disable in `Program.cs` |
|--------|-----------------------------------------|
| **Min‑max** (≥ 0.7) | keep / comment the line `m.AddConstr(E, GRB.GREATER_EQUAL, 0.7, …)` |
| **Gini** | uncomment the block marked **gini** (3 constraints) |
| **D‑Index** | block marked **D‑Index** (3 constraints, **on by default**) |

---

## Repository layout

```
.
├── .gitignore
├── README.md
├── vacstep4.sln          # Visual Studio solution
└── vacstep4/             # C# project root
    ├── Program.cs        # all three phases
    ├── vacstep4.csproj
    ├── origindata/       # raw text data (kept in repo)
    │   ├── qit_20*.txt   # instant vaccination rate  q(i,t)
    │   ├── cil_20.txt    # capacity if group i served at location l
    │   ├── djl_20.txt    # travel minutes from origin j to stop l
    │   ├── vit_20*.txt   # value per dose  v(i,t)
    │   └── nis_20.txt    # scenario demand n(i,s)
    └── output/           # **created at runtime** – solutions land here
```

> **Note** `bin/` and `obj/` are build artefacts and are ignored by Git.

---

## Quick start

```bash
# build the solution
dotnet build vacstep4.sln -c Release

# run (≈ 5 min per phase on a laptop)
dotnet run --project vacstep4 -c Release
```

Results appear in:

* `vacstep4/output/phase1_result.txt`
* `vacstep4/output/week*.txt` (one per week, Phase 2)
* `vacstep4/output/phase3_result.txt`

If a phase stops at its time‑limit you still get the best incumbent solution together with its MIP gap.

---

## Tunable parameters (edit **`Program.cs`**)

| Constant | Meaning | Default |
|----------|---------|---------|
| `D_BOUND` | upper bound on **D‑Index** (0 = off, 1 = no cap) | `0.25` |
| `P1_TimeLimit`, `P2_TimeLimit`, `P3_TimeLimit` | solver limits (s) | `300` |
| `StopServiceMin` | minutes spent at each stop | `30` |
| `DayBudgetMin`   | minutes available per vehicle per day | `480` |
| `Prob[]`         | scenario probabilities | `1/3, 1/3, 1/3` |
| `P1_theta`, `P3_theta` | utility weight in the objective | `6` |

Tips  
* Lower `m.Parameters.MIPGap` (Phase 2) or increase the time limits if you need tighter optimality.  
* Increase `NoRelHeurTime` (Phase 1 & 3) when the first feasible solution is slow to appear.

---

## Reading the output

| Variable | Meaning |
|----------|---------|
| **Phase 1** |
| `W_i_t_s` | doses for group *i* in week *t* (scenario *s*) |
| `A_l_t` | 1 if stop *l* is chosen in week *t* |
| **Phase 2** (week *k*) |
| `y_mjlt` | 1 if vehicle *m* travels *j → l* on day *t* |
| `W_g_v_t_s` | doses for reduced group index *g* by vehicle *v* on day *t* |
| **Phase 3** | same names, now for the full 12‑day horizon |

---

## Common issues

| Symptom | Probable cause | Fix |
|---------|----------------|-----|
| `Status = INF_OR_UNBD` | equity caps too tight | relax `D_BOUND` or disable extra equity blocks |
| First feasible solution slow | heuristics need more time | raise `NoRelHeurTime` |
| Day‑budget exceeded | `djl_20.txt` not in minutes or `DayBudgetMin` too low | check both |

---

## Credits

Code developed by **Zeynep Aydin**, with guidance from  
* *Doç. Dr. Eda Yücel*  
* *Prof. Sibel Salman*  
* *Betül Kayışağoly*  

We gratefully acknowledge the **Gurobi Optimizer** academic license.

---

Feel free to open issues or pull requests if you extend or improve the model.
