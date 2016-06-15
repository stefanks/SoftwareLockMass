
# SoftwareLockMass 
[![Build Status](https://travis-ci.org/stefanks/SoftwareLockMass.svg?branch=master)](https://travis-ci.org/stefanks/SoftwareLockMass)
[![Build status](https://ci.appveyor.com/api/projects/status/ix947xpr77m9b6vw/branch/master?svg=true)](https://ci.appveyor.com/project/stefanks/softwarelockmass/branch/master)
[![Coverage Status](https://coveralls.io/repos/github/stefanks/SoftwareLockMass/badge.svg?branch=master)](https://coveralls.io/github/stefanks/SoftwareLockMass?branch=master)

Software for calibrating mass spectra files based on identified peptides. The spectra files can be in the [mzML](http://www.psidev.info/mzml_1_0_0%20) format, or in the proprietary Thermo raw format. The identified peptides must be in an [mzIdentML](http://www.psidev.info/mzidentml) file, which is a product of a [Morpheus](http://cwenger.github.io/Morpheus/) search.

### Command Line Version Usage

The two required parameters are the paths to spectra and identifications files. 
```shell
SoftwareLockMassCommandLine.exe spectra.mzML identifications.mzid
```

The third (optional) parameter is the intensity cutoff, which limits the considered peaks to having a certain minimum threshold. Higher values speed up the calibration process, but may impact the calibration quality by discarding useful information. Lower values do not necessarily help calibration quality, since low intensity peaks may not be meaningful. The default value is 1e3.

The fourth (optional) parameter is the tolerance in [thomsons](https://en.wikipedia.org/wiki/Thomson_(unit)) for determining if a predicted peak is present in the provided spectra file. The default value is 1e-2.


### GUI Version Usage

Drag and drop is supported for both spectra and identified peptide files. Calibrations on multiple files are done in parallel.

### Requirements

In order to use Thermo raw files as inputs, please download and install the MSFileReader by creating an account at [https://thermo.flexnetoperations.com/control/thmo/login](https://thermo.flexnetoperations.com/control/thmo/login).
