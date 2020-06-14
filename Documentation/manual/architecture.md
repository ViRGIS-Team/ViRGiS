# ​Architecture

## ​Purpose

The purpose of this section is to define the high-level conceptual architecture.

Read this section if you want to understand what components are required for any version of the product.

## Objectives

1. The ability to load a 3D representation of a Georeferenced GIS model in a Virtual Reality world with the ability to move around the representation in an intuitive way,
2. The ability to create entities in the representation at run-time from data located outside of the VR engine - i.e. from the “GIS world’ and using GIS formats, and
3. The ability to add and modify entities in the representation from within the VR world in an intuitive way and for those changes to reflect seamlessly back into the GIS World.

## ​Conceptual Architecture

The basic architecture to meet the Objectives is made of four high-level components:

1. Objective 1 is met by creating a ViRGIS VR application,
2. The ViRGIS App is run on the selected VR device - i.e. the Headset,
3. Objective 2 requires that there is one or more GIS Backend(s) to the system and that the ViRGIS app is linked to the GIS Backend(s). Objective 3 requires that the link(s) to the GIS Backend are transactional, and
4. The three objectives together require that there is a high-level architectural component orchestrating the data into the platform. In particular, in GIS the idea of the “Project View”, i.e. how the data entities are combined, is usually separated from the data about the actual entities, i.e. the geometry, and the former includes the presentation or symbology of the entities. Therefore, this orchestrating component is mostly about the handling of the “project” and “symbology” level interactions.

Each of these components will be explained for each version in the next sections.

![image](../images/architecture.png)

Figure 1 - High-Level Conceptual Architecture


## ​ViRGIS App

The ViRGIS app will be created in Unity and will be written in C#.

The ViRGIS app will be compiled for Windows, macOS and Android. The latter two will be compiled on Mono.

The App will be made up of the following components:

1. **Project Schema**. This JSON based schema defines the data structure for the definition of the project.
2. **Entity Data Schema and Object Schema**. This defines how entities will be stored and mapped internally to the VR world.
3. **Data Ingestion**.
4. **Georeference framework**. This creates a mapping between real-world coordinate systems and the VR-world coordinate system.
5. **3D Geometry Tools**. The tools that are required to manipulate the entities in 3D and provide usual GIS functionality.
6. **Event System**. The ViRGIS entities have a system to propagate events through the entity model.
7. **User Interface**. All the details relating to how to represent the user in the VR-world and the UI, Menus and controls to allow them to work in that world.
8. **Symbology and rendering**. This is not part of the App. This is defined in the Core and Pro packages.

## ​VR Adapter

The Core product includes the definition of a JSON schema for the transfer of the Project and Symbology data to the app. The ViRGIS app will read this data from a file.

This is will include the simplest symbology:

*   The ability to choose the colour, shape and size for point and line features , and
*   The ability to choose the colour and size for the perimeter of a polygon and the ability to choose a colour or texture for the mesh of a polygon, with the texture being a .PNG or .JPEG file.

It is assumed that any user of the Core release will develop their own solutions to create this file.

## ​GIS Backend

The Core product will be able to load, edit and save vector layers in the form of standard GeoJSON files using EPSG:4326 ( WGS84, Geographic) CRS.

The Core product will also be able to load georeferenced point clouds as .PLY format files and georeferenced Meshes as .OBJ, .OFF & .STL format files.

The Core product will only load raster tile layers, DEMs and vector tile layers provided by MAPBOX.
