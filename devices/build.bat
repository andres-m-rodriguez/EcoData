@echo off
cl /std:c++17 /EHsc /I include /I vendor src\main.cpp /Fe:ecodata.exe ws2_32.lib
