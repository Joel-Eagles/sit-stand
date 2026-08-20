# sit-stand
I hesitated to post this as i didn't really do anything other than prompt Claude for it but here it is.

This program is not signed and never will be. my bad.

a crappy little sit stand timer for windows to remind you to adjust your standing desk
This is a simple program that gives you a notification on an interval you can set, as well as a tray icon for monitoring the current state and timer.

The tray icon has 3 states, a purple D, a green U, and a grey II (D for down, U for up, II for pause). It is recommended to drag the tray icon to the visible portion of your tray.

right clicking on the tray icon will give you a few options
<ul>
  <li>Switch Now - instantly moves to the next step in the interval and resets the timer</li>
  <li>Pause - pauses the timer and keeps the state how it is</li>
  <li>intervals > - select from a few pre-set intervals</li>
  <li>custom... - allows you to set a custom sit / stand time in minutes</li>
  <li>Tray Icon > - allows you to modify the labels and colors of standing and sitting positions</li>
  <li>Exit - kills the process</li>
  <li>setting the custom... interval will create a config.txt file in %localappdata%/sitstandtimer with the values. you can directly modify the txt if you like</li>
</ul>

<b>Instructions for Automatic Install</b>
<ol>
  <li>Go to releases on the right side in github</li>
  <li>download the zip file</li>
  <li>unzip</li>
  <li>run install.bat</li>
</ol>

<b>Instructions for Manual Install</b>
<ol>
  <li>Download</li>
  <li>unzip</li>
  <li>open %localappdata% in your file explorer</li>
  <li>make a new folder named sitstandtimer</li>
  <li>copy sitstandtimer.exe into the new folder</li>
  <li>right-click the exe and and create a shortcut (show more options on win11)</li>
  <li>in the file explorer window, go to the directory shell:startup</li>
  <li>copy the shortcut to this folder</li>
  <li>Note: this program is completely portable so you can also just double click it to run it without moving it or installing it or anything</li>
</ol>

https://app.any.run/tasks/eafcbf0b-49d6-4264-8b6d-20dfd6579013
