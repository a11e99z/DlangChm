# DlangChm
[CHM](https://en.wikipedia.org/wiki/Microsoft_Compiled_HTML_Help) docs for [Dlang HTML docs](https://dlang.org/documentation.html)

I like CHM-help because help in one compressed file only and you can copy and view it without any tools (I mean for Windows)

I used [Microsoft HTML Help Workshop](https://www.microsoft.com/en-us/download/details.aspx?id=21138) for building CHM.
It allows include Index and Full-Text searching to result file.

C#/.NetCore3 prj transforms D-htmls from DMD distro (DMD/htmls/d/) to acceptable form by HTMLWorkshop/CHM:
- it removes top menu - dont needed for offline help.   
  I draw bar with D-logo and Dlang version with copyrights instead.
- also removes left tree menu - dont needed cuz CHM has own one.
- removes some files like Resources, Issues, Community, Donate etc.
- replaces href-s to non existing files or WWW by simple inner text.
- removes all scripts except listanchors & jQuery. imo anchor links is good/useful feature.
- generates Index for CHM as href-text to Index item.
- generates 4 books contents HHC for HTML Workshop.

Drawbacks:  
- still need manual work for compositing total HHC/contents and to fix some html-files.  
well, such work takes 15 minutes.
- need review one big C# files to some logic parts.

Noticed:   
- each Phobos files contains full tree of links to all of other Phobos files.   
when EXE removes left and top trees new html-files takes 15MB instead 80MB.
- some files have not been updated for a very long time.

Result:   
one compressed 4MB help with Index and Full-Text searching instead 85MB 800 files.

## Description

"**DlangChm**" folder contains C#/NetCore3 project.   
You can compile it in Linux but you still need to run HTML Help Workshop in some virtual machine or Wine or separate Windows machine.

For "**lab**" folder see lab/readme.md

"**releases**" folder contains precompiled CHM files (in future will be more versions) and screenshots:  
![D 2.088.0 tree](../../releases/D-chm-01.jpg)  
![D 2.088.0 full-text search](../../releases/D-chm-02.jpg)  
![D 2.088.0 index](../../releases/D-chm-03.jpg)  
**TODO** sorry, idk how to ref images right. u can see it in release folder

