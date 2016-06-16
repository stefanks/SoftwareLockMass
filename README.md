
# SoftwareLockMass 
[![Build Status](https://travis-ci.org/stefanks/SoftwareLockMass.svg?branch=master)](https://travis-ci.org/stefanks/SoftwareLockMass)
[![Build status](https://ci.appveyor.com/api/projects/status/ix947xpr77m9b6vw/branch/master?svg=true)](https://ci.appveyor.com/project/stefanks/softwarelockmass/branch/master)
[![Coverage Status](https://coveralls.io/repos/github/stefanks/SoftwareLockMass/badge.svg?branch=master)](https://coveralls.io/github/stefanks/SoftwareLockMass?branch=master)

Software for calibrating mass spectra files based on identified peptides. The spectra files can be in the [mzML](http://www.psidev.info/mzml_1_0_0%20) format, or in the proprietary Thermo raw format. The identified peptides must be in an [mzIdentML](http://www.psidev.info/mzidentml) file. Only mzIdentML files produced by a [Morpheus](http://cwenger.github.io/Morpheus/) search (version 255 and above) are currently supported.

### Command Line Version Usage

The two required parameters are the paths to spectra and identifications files:
```shell
SoftwareLockMassCommandLine.exe spectra.mzML identifications.mzid
```

The third (optional) parameter is the intensity cutoff, which limits the considered peaks to having a certain minimum threshold. Higher values speed up the calibration process, but may impact the calibration quality by discarding useful information. Lower values do not necessarily help calibration quality, since low intensity peaks may not be meaningful. The default value is 1e3.

The fourth (optional) parameter is the tolerance in [thomsons](https://en.wikipedia.org/wiki/Thomson_(unit)) for determining if a predicted peak is present in the provided spectra file. The default value is 1e-2.

Specifying all four parameters can be done in the following way:
```shell
SoftwareLockMassCommandLine.exe spectra.mzML identifications.mzid 0 0.032
```

### GUI Version Usage

Drag and drop is supported for both spectra and identified peptide files. Calibrations on multiple files are done in parallel.

## Requirements

In order to use Thermo raw files as inputs, please download and install the MSFileReader by creating an account at [https://thermo.flexnetoperations.com/control/thmo/login](https://thermo.flexnetoperations.com/control/thmo/login).

The following files must be present in the folder with the executable. If not, they are automatically downloaded (to update a file to a newer version, delete it, and the application will download a new version). This is the only network usage by the application. 

* elements.dat: Elements and isotopes masses and abundances 
 
  [http://www.physics.nist.gov/cgi-bin/Compositions/stand_alone.pl?ele=&all=all&ascii=ascii2&isotype=some](http://www.physics.nist.gov/cgi-bin/Compositions/stand_alone.pl?ele=&all=all&ascii=ascii2&isotype=some)
* PSI-MOD.obo.xml: A PTM(Post-translational modification) library

  [http://psidev.cvs.sourceforge.net/viewvc/psidev/psi/mod/data/PSI-MOD.obo.xml](http://psidev.cvs.sourceforge.net/viewvc/psidev/psi/mod/data/PSI-MOD.obo.xml) 
* unimod_tables.xml: A PTM library
 
  [http://www.unimod.org/xml/unimod_tables.xml](http://www.unimod.org/xml/unimod_tables.xml)
* ptmlist.txt: A PTM library
 
  [http://www.uniprot.org/docs/ptmlist.txt](http://www.uniprot.org/docs/ptmlist.txt) 

## License

The software is currently released under the [GNU GPLv3](http://www.gnu.org/licenses/gpl.txt).

Copyright 2016 Stefan S.
