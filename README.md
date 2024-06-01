## PlatformApi
This is a Api for anything Platform releated.

## Documentason

## Setting Up
just add it as a assebaly refrenct and do "using PlatformApi"
then you can acsess it with "PlatformApi.PlatformApi"

## PlatformList
not a funcson but a list of all platforms in the level SOME MIGHT BE NULL! MAKE SURE TO CHECK IF A PLATFORM IS NULL BEFORE YOU USE IT!

## SpawnPlatform
spawns a platform based off of many pramiters.
most of them are self exsplantory so ill menchen the stuff that isnt.
all doubles are rounded to the nearest Thousandnth in order to account for hardwhere difrentses in doubles.
and if you are wondering why not use Fixs drectly its because i want defult values so you dont have to pass in stuff it doesnt even use/have me make 100 funcsons for difrent inputs.
if you would like to set one of the values to something more precice use one of the funcsons below.

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
it only works on platforms made with the platform ability/the SpawnPlatform funcson.

## SetRot
sets the rotatson. rotatson is in radiens.

## GetRot
gets the rotatson. rotatson is in radiens.

## SetMassPerArea
sets mass per area. mass is calculated with the following formula. 10 + MassPerArea * PlatformArea. 
only works on platforms with ResizablePlatform. returns false if it fails

## SetMass
sets mass. only works on platforms WITHOUT ResizablePlatform. returns false if it fails.

## GetMass
gets mass. works on all platform types.

## SetSprite
should be self exsplanitory

## SetColor
should be self exsplanitory

## SetType
sets the platform type. used for drill color and matchomans boulder texstures

## AddAntiLockPlatform
makes the platform a AntiLockPlatform. basicly a platform that moves on a path

## GetAntiLockPlatform
gets the AntiLockPlatform if there is one. returns null if there isnt.

## AddVectorFieldPlatform
makes the platform a VectorFieldPlatform. basicly a platform that moves in a circle/oval

## GetVectorFieldPlatform
gets the VectorFieldPlatform if there is one. returns null if there isnt.

## SetMaterial
should be self exsplanitory

## AddForce
adds a Force

## AddForceAtPosition
adds a Force at the given posison.

## RemovePlatformFancy
if the platform has ResizablePlatform on it, it will remove the platform by strinking it and making it disapear. retruns false if the gameobject doesnt have ResizablePlatform on it.

## GetHome
gets the platforms home (basicly where it wants to be)

## SetHome
sets the platforms home (basicly where it wants to be)

## GetPos
gets the posison of the platform USE THIS INSTEAD OF JUST GETTING THE TRASFORM! 
GETTING THE POS FROM THE TRANSFORM CAN CAUSE DESINKS DUE TO DIFRENCES IN FLOATING POINT IN DIFRENT CPUS!!!

## SetPos
sets the posison of the platform. would recoment also setting the Home or it will just try to go back to it.

## GetScale
gets the scale of the platform

## SetScale
sets the scale of the platform

## SetScaleSmooth
scales the platform smoothly. returns the resulting ScaleChanger object.

## PlatformArea(GameObject platform)
returns area of given platform

## PlatformArea(Fix Width, Fix Height, Fix Radius)
returns area of platform with those prams

## AddShake
shakes the platform.

## GetDPhysicsRoundedRect, GetShakablePlatform, GetStickyRoundedRectangle, GetBoplBody, GetAnimateVelocity, GetSpriteRenderer and GetFixTransform
gets the given component. just to make your code a bit clearer


## License
This mod is released under the Creative Commons Attribution 4.0 license. See the `LICENSE` file for more information.
