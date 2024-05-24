## PlatformApi
This is a Api for anything Platform releated.

## Documentason

## SpawnPlatform
spawns a platform based off of many pramiters.
most of them are self exsplantory so ill menchen the stuff that isnt.

## X and Y cords
Camera_XMin = (Fix)(-97.27f);
Camera_XMax = (Fix)97.6f;
Camera_YMax = (Fix)40f;
waterHeight = (Fix)(-11.3f);
spaceWaterHeight = (Fix)(-50f);

## Width and Height and Radius
Width and Height are odd in that they act like the radius of a circle in that they are the distance from the center to the edge (minus the Radius). the raidus*2 is added to the Width/Height to get the true Width/Height.

## rotatson
rotatson is in radiens.

## MassPerArea
mass is calculated with the following formula. 10 + MassPerArea * PlatformArea.

## UseSlimeCam
if true slime trails will be there.

## ResizePlatform
Width and Height are odd in that they act like the radius of a circle in that they are the distance from the center to the edge (minus the Radius). the raidus*2 is added to the Width/Height to get the true Width/Height.

## SetRot
sets the rotatson. rotatson is in radiens.

## SetMassPerArea
mass is calculated with the following formula. 10 + MassPerArea * PlatformArea.

## SetSprite
should be self exsplanitory

## SetColor
should be self exsplanitory

## SetType
sets the platform type.

## AddAntiLockPlatform
makes the platform a AntiLockPlatform. basicly a platform that moves on a path

## AddVectorFieldPlatform
makes the platform a VectorFieldPlatform. basicly a platform that moves in a circle/oval

## SetMaterial
should be self exsplanitory

## AddForce
adds a Force

## AddForceAtPosition
adds a Force at the given posison.

## SetHome
sets the platforms home (basicly where it wants to be)

## License
This mod is released under the Creative Commons Attribution 4.0 license. See the `LICENSE` file for more information.
