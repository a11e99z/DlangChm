# LAB

Folder contains templates and modified files for right showing/compiling CHM.

Main HTML Help Workshop file is **Dlang.hpp**.  
it contains list of all used files - some of source files dont needed at all.  

**Contents-Templates.hhc** contains template for D-CHM books/leftTree

**empty.page** contains dumb-html that used for CHM-books when no appropriate html

**js** contains all scripts that needed for CHM (delete others)  
listanchor.js is modified cuz native Windows CHM-viewer using IE6 that doesn't support modern features

**part_to_modify.txt** contains list of manual works that should be done before compiling project in HTML Workshop.

next files I copy myself from D/html/d/ to lab for compiling CHM:
		articles	<folder>
		changelog	<folder>
		foundation	<folder>
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
