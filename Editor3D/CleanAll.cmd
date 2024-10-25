
REM Cleanup all intermediate files from Visual Studio

attrib -h *.suo
del *.suo

move Editor3D\Release\Editor3D.exe  Editor3D.exe

rmdir Editor3D\obj /S /Q
rmdir Editor3D\Debug /S /Q
rmdir Editor3D\Release /S /Q
