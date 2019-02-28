#!/bin/sh
DIRS="src app csharp test/fsharp test/csharp"
for dir in $DIRS; do
	rm -rf $dir/bin $dir/obj
done
for dir in $DIRS; do
	dotnet build $dir
done