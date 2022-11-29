# README

## First school project

For this project I had to implement a Windows Forms application which would allow the user to place geometric figures on the form and would draw a convex hull around them. Side functionality includes: changing the size and shape of the figures, deleting them, basic drag and drop, file save/open through binary serialization and attachment of custom DLLs implementing new geometric shapes at runtime.
I had set for myself the intention of learning the basic OOP principles and design patterns. I had to dig into the use of interfaces, abstract classes, inheritance, etc. I used the state pattern and the memento pattern to implement save/load functionality, and the command pattern to implement the undo/redo functionality. Also I had to figure out how to write DLLs and find a way to link them at runtime in order to implement the "DLL drag n drop" functionality, which would allow the user to write their own custom figure classes and drag them onto the windows form at runtime and then be able to use them.
