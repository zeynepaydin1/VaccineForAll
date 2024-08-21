# Vaccines4All Project

## Overview

The Vaccines4All project is a mathematical optimization model developed to solve complex location-routing problems associated with large-scale vaccination efforts. This project aims to distribute vaccines efficiently and fairly, particularly focusing on reaching vulnerable populations during pandemics, such as COVID-19.

The model is implemented in C# using the Gurobi optimization solver, addressing critical challenges in the logistics of vaccine distribution, including facility location, routing of mobile vaccination units, and allocation of limited vaccine supplies.

**Disclaimer:** This repository is for educational and demonstration purposes only. The code and models presented here should not be deployed in real-world scenarios. **No one is permitted to use, distribute, or modify this code without explicit permission from the author.**

## Project Objectives

- **Optimization Model:** Develop a model to optimize the placement of vaccination centers and the routing of mobile units to maximize coverage and minimize expected loss of life.
- **Prioritization and Allocation:** Integrate prioritization scenarios based on medical expertise and logistic constraints to ensure vaccines reach the most at-risk populations effectively.
- **Application:** The model is designed for application in scenarios similar to the COVID-19 pandemic in Turkey and the UK, with a focus on regions with vulnerable populations.

## Key Features

- **Facility Location Optimization:** Determines the optimal locations for vaccination centers based on population distribution and disease spread models.
- **Routing of Mobile Units:** Optimizes the routes for mobile vaccination units to ensure vaccines reach remote or hard-to-access populations.
- **Vaccine Allocation:** Allocates vaccine doses to different regions and facilities, balancing between prioritization criteria and logistic constraints.

## Requirements

- **Gurobi Optimization Solver:** The model utilizes Gurobi for solving the optimization problems. Ensure that Gurobi is installed and properly configured on your system.
- **C# Development Environment:** The project is implemented in C#. You will need a compatible development environment such as Visual Studio.

## Installation

To set up the project locally, follow these steps:

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yourusername/vaccines4all.git
   cd vaccines4all
   
2. **Open the solution:**
- Open the `v4a.sln` solution file in your C# development environment.
  
3. **Configure Gurobi:**
- Ensure Gurobi is installed and licensed on your system. Update the project references to include the Gurobi DLLs if needed.

4. **Build and Run:**
- Build the solution and run the project within your development environment.

## Usage

This project is structured for ease of integration and testing. You can modify the input parameters directly in the source code or through configuration files to test different scenarios.

## License

This project is licensed under a custom restrictive license. No one is permitted to use, distribute, or modify this code without explicit permission from the author.

## Contact

Project Lead: Prof. Dr. Sibel Salman, Koç University, Industrial Engineering Department

