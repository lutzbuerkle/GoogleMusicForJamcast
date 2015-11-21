import os
import sys
import zipfile

prjdir = sys.argv[1].rstrip(' .')
tgtdir = sys.argv[2].rstrip(' .')
filename = sys.argv[3]

os.remove(prjdir + filename + '.jpl');

zip = zipfile.ZipFile(prjdir + filename + '.jpl', 'w', zipfile.ZIP_DEFLATED)
zip.write(tgtdir + filename + '.dll', filename + '.dll');
zip.write(tgtdir + 'GoogleMusic.dll', 'GoogleMusic.dll');
zip.write(tgtdir + 'plugin.xml', 'plugin.xml');
zip.write(prjdir + 'LICENSE', 'LICENSE');

zip.close()
