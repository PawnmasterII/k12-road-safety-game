# Presentation demo guide

## Before the meeting

- Start the FastAPI server on port 8000 and Vite on port 5173 (README commands). Load http://localhost:5173 while online so the browser is ready.
- Open the Unity project at `C:/Users/dongt/Documents/CUTR-K12-Safety-Game/unity`, then `Assets/Scenes/City.unity`.
- Keep the website and Unity Game view in separate windows. Use fictional names and school details only.
- Do not rebuild the city immediately before presenting; use the saved scene. Do not delete the local database.

## Website demonstration

1. Show the homepage and explain the four age-appropriate grade bands.
2. Choose **Play as a guest**. Select a module and start its sample activity.
3. Answer a question, show feedback and the cited source, then complete the activity to show the score.
4. Explain that guest progress is intentionally not saved.
5. If demonstrating persistence, register a fictional account, complete an activity, and show its dashboard score/badges. Log out and back in to show saved progress. Keep any demo PIN private and do not commit it.

## Unity demonstration

1. Press Play, then click inside the Game view.
2. Show movement with WASD / arrow keys, the neighborhood map and fog-of-war.
3. Approach a character and use Space when prompted. Show the dialogue and close it using its on-screen instructions.
4. Enter a learning zone and press Space when prompted, answer a quiz question, show the feedback, then close/finish the quiz and resume moving.
5. Explain that the City is a configurable neighborhood generated from JSON, while SafeStreets is the earlier small crossing test scene.

## Honest progress statement

> We have two working research prototypes: a web learning flow with guest play and persistent student progress, and a Unity neighborhood with exploration, character interaction and safety quizzes. The next integration milestone is a WebGL build linked to the site with authenticated progress hand-off. This is not yet a production school deployment or a validated learning-outcome study.

Do not claim that Unity scores already synchronize with the website, that the game is publicly hosted, or that four-digit PIN authentication is ready for real student data.

## Quick recovery

- **Website says it cannot connect:** check the API terminal is still running on 8000, then reload the page. Do not reset the database.
- **Port is occupied:** reuse the existing working server rather than launching duplicates.
- **Unity does not respond to keys:** click the Game view; close any dialogue/quiz first. Make sure the Editor is not paused.
- **Unity scene is empty:** stop Play and reopen `Assets/Scenes/City.unity` from the Project panel.
- **Unity is stuck:** stop Play and start again. Use website guest play as the fallback while recovering the Editor.

## Scope

Question banks are samples. No teacher dashboard, multilingual UI, hosted WebGL game or verified Unity-to-web authentication is included. Present the website and Unity as separate windows.
