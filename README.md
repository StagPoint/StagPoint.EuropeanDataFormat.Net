[![GitHub issues](https://img.shields.io/github/issues/StagPoint/StagPoint.EuropeanDataFormat.Net.svg)](https://GitHub.com/StagPoint/StagPoint.EuropeanDataFormat.Net/issues/)
<a href="https://github.com/StagPoint/StagPoint.EuropeanDataFormat.Net/blob/master/LICENSE">
<img src="https://img.shields.io/github/license/StagPoint/StagPoint.EuropeanDataFormat.Net" alt="License"/>
</a>
[![Nuget](https://img.shields.io/nuget/v/StagPoint.EuropeanDataFormat.Net)](https://www.nuget.org/packages/StagPoint.EuropeanDataFormat.Net/)
[![Nuget](https://img.shields.io/nuget/dt/StagPoint.EuropeanDataFormat.Net)](https://www.nuget.org/packages/StagPoint.EuropeanDataFormat.Net/)
![GitHub release (with filter)](https://img.shields.io/github/v/release/StagPoint/StagPoint.EuropeanDataFormat.Net)
![GitHub all releases](https://img.shields.io/github/downloads/StagPoint/StagPoint.EuropeanDataFormat.net/total)
[![GitLab last commit](https://badgen.net/github/last-commit/StagPoint/StagPoint.EuropeanDataFormat.net/)](https://github.com/StagPoint/StagPoint.EuropeanDataFormat.net/-/commits)

# StagPoint.EuropeanDataFormat.Net
#### .NET library for reading and writing European Data File (EDF) format files

<!-- TOC -->
* [StagPoint.EuropeanDataFormat.Net](#stagpointeuropeandataformatnet)
      * [.NET library for reading and writing European Data File (EDF) format files](#net-library-for-reading-and-writing-european-data-file-edf-format-files)
* [Summary](#summary)
* [Binary Distribution](#binary-distribution)
* [European Data Format](#european-data-format)
  * [The European Data File Specification](#the-european-data-file-specification)
      * [ASCII-format fields](#ascii-format-fields)
  * [Header Record Data Structure](#header-record-data-structure)
    * [Header Record - Fixed-sized portion](#header-record---fixed-sized-portion)
    * [Header Record - Signal Information](#header-record---signal-information)
<!-- TOC -->

# Summary

This library implements the European Data Format (EDF) and associated EDF+ specifications in a purely managed .NET library, written entirely in C#. 
It supports reading, writing, and modifying standard EDF format files, and supports EDF+ extensions such as Annotations and discontinuous files.
Usage examples can be found in the StagPoint.EDF.Tests unit testing project. This project is provided under the terms of the [MIT license](http://choosealicense.com/licenses/mit/).

# Binary Distribution

The easiest way to make use of this library in your own project is to add a reference to the following [NuGet package](https://www.nuget.org/packages/StagPoint.EuropeanDataFormat.Net/).

---

# European Data Format

European Data Format (EDF) is a standard file format designed for exchange and storage of medical time series. Being an open and non-proprietary format, EDF(+) is commonly used to archive, exchange and analyse data from commercial devices in a format that is independent of the acquisition system. In this way, the data can be retrieved and analyzed by independent software. EDF(+) software (browsers, checkers, ...) and example files are freely available.

EDF was published in 1992 and stores multichannel data, allowing different sample rates for each signal. Internally it includes a header and one or more data records. The header contains some general information (patient identification, start time...) and technical specs of each signal (calibration, sampling rate, filtering, ...), coded as ASCII characters. The data records contain samples as little-endian 16-bit integers. EDF is a popular format for polysomnography (PSG) recordings.

EDF+ was published in 2003 and is largely compatible to EDF: all existing EDF viewers also show EDF+ signals. But EDF+ files also allow coding discontinuous recordings as well as annotations, stimuli and events in UTF-8 format. EDF+ has applications in PSG, electroencephalography (EEG), electrocardiography (ECG), electromyography (EMG), and Sleep scoring. EDF+ can also be used for nerve conduction studies, evoked potentials and other data acquisition studies.

## The European Data File Specification

The following was adapted from the official EDF and EDF+ specifications, which can be found online at [EDF Full Specification](https://www.edfplus.info/specs/edf.html) and [EDF+ Full Specification](https://www.edfplus.info/specs/edfplus.html).

One data file contains one uninterrupted digitized polygraphic recording. A data file consists of a header record followed by one or more data records. The variable-length header record identifies the patient and specifies the technical characteristics of the recorded signals. The data records contain consecutive fixed-duration epochs of the polygraphic recording.

The duration of each data record is recommended to be a whole number of seconds and its size (number of bytes) is recommended not to exceed 61440. Only if a 1s data record exceeds this size limit, the duration is recommended to be smaller than 1s (e.g. 0.01).

The digital minimum and maximum of each signal should specify the extreme values that can occur in the data records. These often are the extreme output values of the A/D converter. The physical (usually also physiological) minimum and maximum of this signal should correspond to these digital extremes and be expressed in the also specified physical dimension of the signal. These 4 extreme values specify offset and amplification of the signal.

Following the header record, each of the subsequent data records contains 'duration' seconds of 'ns' signals, with each signal being represented by the specified (in the header) number of samples. In order to reduce data size and adapt to commonly used software for acquisition, processing and graphical display of polygraphic signals, each sample value is represented as a 2-byte integer in 2's complement format. 

Gains, electrode montages and filters should remain fixed during the recording. Of course, these may all be digitally modified during replay of the digitized recording.
Below is the detailed digital format of the header record (upper block, ascii's only) and of each subsequent data record (lower block, integers only). Note that each one of the ns signals is characterized separately in the header.

#### ASCII-format fields

All header fields are stored in [ASCII format](https://en.wikipedia.org/wiki/ASCII). The information in the ASCII strings must be left-justified and right-padded with spaces.

---

## Header Record Data Structure

The first 256 bytes of the header record specify the version number of this format, local patient and recording identification, time information about the recording, the number of data records and finally the number of signals (ns) in each data record. 

### Header Record - Fixed-sized portion

| Size | Name                           | Format         | Description                                                                                                                                     |
|------|--------------------------------|----------------|-------------------------------------------------------------------------------------------------------------------------------------------------|
| 8    | Version                        | ASCII Integer  | Version of this data format (always 0)                                                                                                          |
| 80   | Local Patient Identification   | ASCII String * | Either a unique identifier for the patient (in EDF), or a group of subfields containing more detailed patient information (EDF+)                |
| 80   | Local Recording Identification | ASCII String * | Either a unique identifier for this recording (in EDF), or a group of subfields containing more detailed information about the recording (EDF+) |
| 8    | Start Date of Recording        | ASCII Date     | The start date (in dd.mm.yy format) for when this recording was created                                                                         |
| 8    | Start Time of Recording        | ASCII Time     | The time (in hh.mm.ss format) when this recording was started                                                                                   |
| 8    | Header record size             | ASCII Integer  | The size (in bytes) of this header record                                                                                                       |
| 44   | Reserved                       | ASCII String   | Reserved for use in future versions                                                                                                             |
| 8    | Number of Data Records         | ASCII Integer  | The number of data records stored in the file                                                                                                   |
| 8    | Duration of Data Records       | ASCII Float    | The amount of time (in seconds) represented by each Data Record                                                                                 |
| 4    | Number of Signals              | ASCII Integer  | The number of signals whose data is stored in each Data Record                                                                                  |

### Header Record - Signal Information
Immediately following the fixed-sized portion of the header record will be a number of arrays describing the characteristics of each stored signal.

Each array will have **NS** (number of signals) elements, in the same order in which the Signals are stored. 

| Size    | Name               | Format        | Description                             |
|---------|--------------------|---------------|-----------------------------------------|
| NS * 16 | Label              | ASCII String  | e.g. EEG Fpz-Cz or Body temp            |
| NS * 80 | Transducer Type    | ASCII String  | e.g. AgAgCl electrode                   |
| NS * 8  | Physical Dimension | ASCII String  | e.g. uV or degreeC                      |
| NS * 8  | Physical Minimum   | ASCII Integer | e.g. -500 or 34                         |
| NS * 8  | Physical Maximum   | ASCII Integer | e.g. 500 or 40                          |
| NS * 8  | Digital Minimum    | ASCII Integer | e.g. -2048                              |
| NS * 8  | Digital Maximum    | ASCII Integer | e.g. 2047                               |
| NS * 80 | Prefiltering       | ASCII String  | e.g. HP:0.1Hz LP:75Hz                   |
| NS * 8  | Number of Samples  | ASCII Integer | The number of samples per Data Record   |
| NS * 32 | Reserved           | ASCII String  | Reserved for future use                 |


