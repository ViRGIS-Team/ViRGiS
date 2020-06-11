# Virgis Entity Model

## Conceptual Data Model

The high level entity structure, in line wit standard GIS practice is based on the following model.

![ConceptualModel](../images/conceptual_entity.png )

Where :



*   A Feature is a datapoint with some geometry, and
*   A Layer is a set of Features that  :
    *   Are all associated with some metadata about how to represent those features - i.e size and shape of point or line, color or point or line or polygon etc. This says that all the Features in one Layer all look the same, and 
    *   All mean the same thing and came from the same source - e.g. a set of points each of which is a place where a rock sample was taken or an archeological pot was found OR a set of lines each of which is a fault line in the rock.


## Logical Entity Model

The conceptual model is implement using the following model - again broadly in line with best practice.


![alt_text](../images/logical_entity.png "image_tooltip")


Where:



*   FeatureCollection, Feature and Geometry are defined as per the definitions in GeoJSON ( see [https://tools.ietf.org/html/rfc7946](https://tools.ietf.org/html/rfc7946)) 
*   A Unit is an atomic unit of Symbology define the size, shape, colour and offset position for one component of the feature(for instance lines or points)
*   Symbology is the metadata that complete describes how the features in a layer should be represented,
*   A RecordSet is the complete representation of a layer, and
*   A Project is a complete representation of a model.

Unit, RecordSet and Project are defined in the Scripting Reference.


## Current Implementation

The Logical Entity Model is implemented by two files and two types of GameObject, as follows

![image](../images/physical_entity_1.png )


Where the source is a reference to one GeoJSON file


![image](../images/physical_entity_2.png )


All Feature types implement the IVirgisFeature interface and extend VirgisFeature. There can be as many Feature types as there are types of geometry.

All Layer types implement the IVirgisLayer interface and extend VirgisLayer. There can be as many Layer types as required.

The Map GameObject is an instance of a Layer that happens to contain other layers.

