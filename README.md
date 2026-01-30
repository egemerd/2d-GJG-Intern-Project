# GJG Case Study: Collapse/Blast Mechanic

This project was developed with a primary focus on **high-performance engineering**, specifically optimizing memory, CPU, and GPU usage to ensure a "healthy and efficient heart" for the gameplay experience.

---

##  Performance Optimization
* **Draw Call Reduction:** Utilized **Sprite Atlases** to batch textures and minimize GPU overhead.
* **Memory Management:** Implemented an **Object Pooling System** for blocks and particle effects to prevent Garbage Collection (GC) spikes.
* **State Machine Management:** Orchestrates transitions between game states to ensure logical execution order and lock player input during cascading operations like blasting and refilling.

---

##  Design Patterns
* **Facade Pattern (`GridManager`):** Acts as the central hub to coordinate and manage all specialized subsystems, ensuring a modular and organized game loop.
* **Observer Pattern:** Leverages **C# events** to decouple systems (e.g., `BlastSystem`, `GravitySystem`), allowing them to communicate without direct dependencies.
* **Data-Driven Design:** Uses **ScriptableObjects** for level setups, enabling easy adjustment of rows ($M$), columns ($N$), and color counts ($K$).



---

##  Intelligent Deadlock Resolution
To avoid "blindly shuffling," the game features a strategic **Shuffle System** that guarantees a playable state.

* **Guaranteed Match Selection:** * The system filters colors that meet the minimum requirement of at least 2 blocks.
    * It selects a specific number of "guaranteed colors" to ensure the new grid is immediately playable.
* **BFS Cluster Formation:** * Uses a **Breadth-First Search (BFS)** algorithm to find adjacent vacant positions for each guaranteed color.
    * These positions are reserved to prevent overlap and ensure the match is preserved during generation.
* **Procedural Filling:** * Collects all remaining non-reserved positions.
    * Applies a **Fisher-Yates Shuffling** algorithm to randomize and assign the remaining blocks.



---

##  Level Design Tooling
Developed a custom editor tool suite to enable designers to easily create and iterate on level layouts.

* **Custom Editor Window:** A dedicated interface to streamline the initial level creation process for designers.
* **Workflow Optimization:** Features a suite of tools including **Paint, Fill, and Erase** to allow designers to manually arrange grid configurations with precision.
* **Designer-Centric Approach:** A flexible solution that empowers the team to iterate quickly on level layouts and color distributions.

---

