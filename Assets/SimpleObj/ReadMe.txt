SimpleObj
---------
Import Wavefront models into Unity3D at runtime. SimpleObj provides the tools to let users upload 3D models 
and use them in your game.

No fancy stuff, no nonsense. It imports vertices, normals, 1 uv map, and triangles. (polygons are 
automatically converted to triangles). Supports multiple meshes and sub meshes.

Just download a OBJ file into a string, pass the string to the import function and receive a GameObject in return. Like so:
	// import an OBJ file that is read into a string.
	myGameObject = ObjImporter.Import(objString);


Documentation
-------------
The full documentation is available online at http://orbcreation.com/orbcreation/docu.orb?1097


Package Contents
----------------
SimpleObj / ReadMe.txt   (this file)
SimpleObj / ColladaImporter.cs   (the Collada importer)
SimpleObj / MeshExtensions.cs   (class extensions for Mesh)
SimpleObj / CollectionExtensions.cs   (class extensions for Hashtable and ArrayList)
SimpleObj / StringExtensions.cs   (class extensions for string to read arrays of floats or vector)
SimpleObj / Demo / SimpleObjDemo.unity   (the demo scene)
SimpleObj / Demo / Examples.cs (example code)
SimpleObj / Demo / SimpleObjDemoCtrl.cs (script that runs the demo)
SimpleObj / Demo / Materials (materials used for floor and background)
SimpleObj / Demo / Shaders (simple shaders for text and background)
SimpleObj / Demo / Textures (grid texture for the floor, grid texture for the models, background image)


Minimal needed in your project
------------------------------
The following files need to be somewhere in your project folders:
- ColladaImporter.cs
- CollectionExtensions.cs
- MeshExtensions.cs
- StringExtensions.cs
All the rest can go.


C# and Javascript
-----------------
If you want to create a Javascript that uses the SimpleObj package, you will have to place the scripts in the "Standard Assets", "Pro Standard Assets" or "Plugins" folder and your Javascripts outside of these folders. The code inside the "Standard Assets", "Pro Standard Assets" or "Plugins" is compiled first and the code outside is compiled in a later step making the Types defined in the compilation step (the C# scripts) available to later compilation steps (your Javascript scripts).


