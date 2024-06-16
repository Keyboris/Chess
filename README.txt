PROJECT CHESS IN C#

Semester project of Ivan Yaretskyi.

###################################

Table of contents:
1.Description.
2.Prerequisites
3.Usage
4.Overview
5.Contribution
6.Contacts

###################################


#### 1.DESCRIPTION

	This is a simple project in c#, build using gtk#. The user plays for white against an ai, which plays for black.
	It is also possible to turn the ai off to play with a friend on one device by a tiny modification in code. The game doesnt support draw by repetition.


#### 2.PREREQUISITES

	To run the program you need the following:

		1. .NET of version 7.0 or above. For more detailes visit https://dotnet.microsoft.com/en-us/download
		2. gtk# installed.
		3. Some sort of compiler to get the executable file.
		4. Optionally, IDE or text editor if you want to contribute.

	If you are using .NET CLI, execute dotnet restore, or just download the gtk# nugget package if you are using Visual Studio Code or VS Code.


#### 3.USAGE

	To play the game, compile the project and run the .exe file. You will be playing for white, when some piece is selected, allowed moves are indicated by blue
	cells. Mate is indicated in a similar way. To disable the ai and play with a friend on one device, in a controller.cs file, find the "HandlePressChessBoard" 
	function in the bottom half of the file, and comment out the "GLib.Idle.Add(() =>" block, which is the following 7 lines (originally, those are the lines 566 - 573).
	After this, the other person will be able to make moves for black, the allowed move sytem works the same.


#### 4.OVERVIEW

	The whole program consists of 4 files: Controller.cs, Agent.cs, Pieces.cs and View.cs. 

	In the Pieces.cs file we have all the info about the pieces.
	The is an abstract class Piece, and all the actual pieces inherit from it. If you would like to add some custom pieces, make sure to make it an child class of 
	a Piece class, so that all the game's functions will work proerly. For the minimax to not take 300 hours each computation, the move-unmove system is used. 
	Each instance of each piece must have a stack of past moves, from which we can pop and easily find the past position of the piece. Make sure to not change the 
	location of the Pieces folder in a project, the Piece class has a function which locates the right picture from the specific address.

	In the Controller.cs, the logic of the game is implemented. Firstly, we have a board class. After this, a huge class called Controller. The controller is
	assigned to nearly every other component of a program, since it holds things like board state, game state, functions like checkCheck and AllowedMove, etc.
	If you would like to add some custom pieces to the game, you should change the init of a controller, specifying the positions of new pieces on board. Below that
	there is a Move method, which controlls the logic of the move, checks the game state after the move is done, checks if the move is legal and updates the UI.
	Because of the last feature, in any other place, like in an ai implementation, I have used a Piece method Move_to rather than Move in a controller. Controller also
	contains implementations of methods AllowedMove, checkCheck, and mateCheck. The checkCheck works the following way: we locate the king, look in every direction and
	see if we are in danger. We also find all the knights of the opposite color and take them into consideration. Previous versions of the game used to have some separete
	variables that used to hold the positions of the king and all of the knights of a certain color on the board, but i have decided to reject this idea, since it is 
	extremely easy to make a mistake, espcially with knights. So the decision was made to o with a slowed but safer solution. Alowed move checks if the player
	is in check and if they will stay in check after the move is done. Next we have press handlers which get assigned to different elements of UI.
	The "HandlePressChessBoard" is just your normal press handler, while "HandlePressChoiseBox" is invoked after the player has clicked on the box
	which pops up after a promotion and offers a player some pieces to make out of their pawn.

	In the View.cs, the UI is implemented using gtk#. The base widget in the main window must be an overlay, since we would have different widgets popping out one on
	top of another sometimes. The main board is realised using a table and a custom widget Cell, each instance of which has a controller tied to it. In the program
	class we also have methods which will show the choice box when promoting and hide it when we have chosen some piece. 

	In the agent.cs we have the implementation of an ai to play against. The initial depth of the minimax is set to 2, it can be changed by changing 
	the MaxDepth constant in the agent class. Since the agent has to be able to use some of the controller's functions, such as checkCheck or AllowedMove,
	and since the controller has to be able to call the GetBestMove from the agent at the same time, there obviously is a bit of an mutual dependency. 
	I have tried the observer solution, but in the end have decided to cheat a bit and create a separete method which allows an agent to be assigned
	with a controller. The minimax uses alpha beta pruning. The evaluation function is agnostic, it uses 12 position tables, one for each piece of each color,
	as well as the pawn chain evaluation and assignes a bonus for an open lane. In general, the result feels super defencive. If no advances from the player are made,
	it will just repeat moves until the player does something. When promoting, the ai is hardcoded to chose a queen.


#### 5.CONTRIBUTION
	
	The project is open source, though protected by a MIT Licence. Feel free to copy and make changes to the project as you wish. Please contac me if you find any
	bugs.

#### 6.CONTACTS
	email: iluyaretskyi@gmail.com

	