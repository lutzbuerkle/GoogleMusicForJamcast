use strict;
use Archive::Zip qw(:ERROR_CODES :CONSTANTS);

our $WScript;
my $arg = $WScript->{Arguments};

my $prjdir = $arg->Item(0);
my $tgtdir = $arg->Item(1);
my $file = $arg->Item(2);

unlink($prjdir.$file.'.jpl');

my $zip = Archive::Zip->new();
$zip->addFile($tgtdir.$file.'.dll',$file.'.dll');
$zip->addFile($tgtdir.'GoogleMusic.dll','GoogleMusic.dll');
$zip->addFile($tgtdir.'plugin.xml','plugin.xml');
$zip->addFile($prjdir.'LICENSE','LICENSE');

my $status = $zip->writeToFileNamed($prjdir.$file.'.jpl');
exit $status;
