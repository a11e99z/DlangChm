# LAB

Folder contains templates and modified files for right showing/compiling CHM.

Main HTML Help Workshop file is **Dlang.hpp**.  
it contains list of all used files - some of source D-html files dont needed at all.  

**Contents-Templates.hhc** contains template for D-CHM books/leftTree.  
copy it to new Contents.hhc (that refs prj) and modify it as **part_to_modify.txt** says

**empty.page** contains dumb-html that used for CHM-books when no appropriate html.  
each CHM book/left-tree-group-item should refs to some useful html.

**js** contains all scripts that needed for CHM (delete others)  
listanchor.js is modified cuz native Windows CHM-viewer using IE6 that doesn't support modern JS-features.

**part_to_modify.txt** contains list of manual works that should be done before compiling project in HTML Workshop.

**part_to_modify.txt** contains list of manual works that should be done before compiling project in HTML Workshop.

next files I copy myself from D/html/d/ to lab for compiling CHM:
~~~~
	articles	<folder>
	changelog	<folder>
	css			<folder>
	foundation	<folder>
	images		<folder>
	phobos		<folder>
	spec		<folder>
	acknowledgements.html
	areas-of-d-usage.html
	ascii-table.html
	comparison.html
	concepts.html
	deprecate.html
	dmd.html
	dmd-freebsd.html
	dmd-linux.html
	dmd-osx.html
	dmd-windows.html
	dstyle.html
	glossary.html
	gpg_keys.html
	howto-promote.html
	htod.html
	install.html
	overview.html
	rdmd.html
	security.html
	tuple.html
	wc.html
	windbg.html
~~~~
