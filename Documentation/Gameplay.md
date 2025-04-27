# Gameplay Overview (Player Perspective)

This document describes the expected gameplay experience from the perspective of someone playing the text-based RPG.

## Core Experience

*   **Genre:** Text-Based Role-Playing Game (RPG).
*   **Interaction:** You play the game by typing commands in natural language (e.g., "look around", "talk to the innkeeper", "examine the chest", "attack the goblin").
*   **AI Dungeon Master (DM):** The game world and narrative are managed by an AI that acts like a Dungeon Master. It interprets your commands, describes the environment, controls NPCs, narrates events, and determines the outcomes of your actions.
    *   **Narrative Style:** Expect the DM to have a slightly witty or snarky persona, like a friend running the game, while still matching the overall tone (e.g., fantasy) of the game setting. Descriptions aim to be immersive and detailed.
*   **Dynamic World:** The world reacts to your actions. NPCs remember interactions (to some extent), locations can change, and your choices influence the unfolding story.
    *   **Time:** The game world has its own clock. Significant time (minutes, hours, days) can pass based on your actions (resting, traveling, waiting, studying) and will be noted by the DM.
    *   **Random Events:** Be prepared for occasional unexpected events introduced by the DM to add spontaneity to the narrative.

## Key Gameplay Mechanics

*   **Exploration:** You can move between different locations like settlements, buildings, wilderness areas, and potentially dungeons or caves ("delves"). The DM describes what you see and encounter in each location.
*   **Dialogue:** You can talk to NPCs. This is typically handled through dedicated commands or UI elements specifically for NPC interaction (e.g., selecting an NPC and choosing dialogue options or typing what you say *to them*). Trying to relay dialogue *through* the main DM command input (e.g., "I ask the blacksmith if he has seen the fugitive") will likely result in the DM prompting you to interact with the NPC directly.
*   **Quests:** You can receive and undertake quests, which provide goals and structure to your adventure. Progress is tracked, and completing quests likely yields rewards and story progression.
*   **Combat:** You will encounter enemies and engage in turn-based combat.
    *   **Initiation:** Combat can be triggered by narrative events (like an ambush described by the DM) or by your direct actions ("I attack the guard!"). The DM will describe the start of the fight.
    *   **Turns:** Once combat begins, you'll take turns performing actions (attacking, using skills/items, defending) via specific combat commands.
    *   **Results:** Combat ends when one side is defeated.
    *   **Summary:** A summary of the combat outcome is provided.
*   **Inventory Management:** You have an inventory where you store items and currency found during your adventures. You can likely use commands to manage items (e.g., "check inventory", "use health potion", "equip sword"). Items can be crucial for solving problems or succeeding in Task Resolution (see below).
*   **Character Progression & RPG Tags:** Character improvement primarily comes from acquiring better equipment and earning **RPG Tags**.
    *   **RPG Tags:** These represent specific skills, knowledge, or significant experiences/achievements (e.g., `Lockpicking`, `Herbalism`, `Survived Ambush`, `Knows Cave Shortcut`). They are earned through your actions and discoveries.
    *   **Usage:** Tags are key in the **Task Resolution** system, allowing you to reduce the difficulty of relevant challenges.
*   **Task Resolution (Non-Combat Challenges):** When you attempt a non-trivial action with an uncertain outcome (like climbing a tricky wall, sneaking past guards, persuading someone *via the DM*, searching for hidden clues, resisting an effect), the game uses a dice roll mechanic:
    1.  **GM Assessment:** The DM determines if a roll is needed or if the action is automatic.
    2.  **Base Difficulty:** The DM announces the base difficulty (1-10) for the task.
    3.  **Modifiers:** Difficulty might increase due to negative conditions (injuries, bad weather, time pressure). The DM explains why.
    4.  **Player Input (Reducing Difficulty):** You can propose ways to make the task easier. Each valid proposal reduces the final difficulty by 1:
        *   Use a relevant **RPG Tag** you possess (e.g., using `Stealth` tag to sneak).
        *   Use an appropriate **Item** from your inventory.
        *   Leverage the **Environment** cleverly (e.g., using debris for cover).
    5.  **Final Difficulty & Roll:** The DM confirms the final difficulty after your input. Success requires rolling a 20-sided die (d20) and getting a result **equal to or higher than (Final Difficulty x 3)**.
*   **Time:** The game world has its own clock. Time progresses as you take actions and events occur. The DM will narrate significant time passage.

## Game Flow

1.  **Start:** You likely begin in a starting scenario or location.
2.  **Interact:** You type commands to explore, talk, fight, or perform other actions.
3.  **Response:** The AI DM processes your command and responds with a narrative description of the outcome, updating the world state.
    *   **Narrative Pacing:** If you declare an ongoing action (e.g., "I travel north," "I search the library," "I keep watch"), the DM will narrate a meaningful segment of that activity, including time passage or observations, until a natural break, discovery, or interruption occurs. You don't need to constantly re-state your ongoing action.
    *   **Player Prompts:** The DM will generally only explicitly ask "What do you do?" or prompt for input when a clear **decision point** is reached (e.g., facing a sudden threat, discovering something crucial, encountering a significant obstacle or choice presented in the narration). Otherwise, you can interject with new actions whenever you wish.
4.  **Repeat:** You continue interacting, driving the story and exploring the world.
5.  **Persistence:** Your progress (character state, inventory, world changes, quest status) is saved automatically, allowing you to continue your game later.

## Goal

The primary goal is to immerse yourself in the role of your character, explore the game world, interact with its inhabitants, overcome challenges, complete quests, and experience a dynamic, AI-driven story shaped by your decisions.
