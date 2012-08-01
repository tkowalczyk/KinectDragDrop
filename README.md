KinectDragDrop
==============

This simple application shows how you can implement Dag and Drop feature in applications based on [Kinect for Windows SDK](http://www.microsoft.com/en-us/kinectforwindows/ "Kinect for Windows SDK") in the WPF. I realize that this is not the best solution but I hope that it is good proof of concept.


To implement Drag and Drop feature we have to define some helpers flag:


`private bool isGreenClicked = false;`
`private bool isBlueClicked = false;`
`private bool isGreenAimClicked = false;`
`private bool isBlueAimClicked = false;`

The default flag from Kinect Hover Button control from Channel9 team is not enough because it could be set only once when it is clicked. In this implementation you could drop specific item only on properly set target.