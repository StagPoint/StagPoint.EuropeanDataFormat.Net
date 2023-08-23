<a name='assembly'></a>
# StagPoint.EDF.Net.v2

## Contents

- [EdfAnnotation](#T-StagPoint-EDF-Net-EdfAnnotation 'StagPoint.EDF.Net.EdfAnnotation')
  - [Annotation](#P-StagPoint-EDF-Net-EdfAnnotation-Annotation 'StagPoint.EDF.Net.EdfAnnotation.Annotation')
  - [Duration](#P-StagPoint-EDF-Net-EdfAnnotation-Duration 'StagPoint.EDF.Net.EdfAnnotation.Duration')
  - [IsTimeKeepingAnnotation](#P-StagPoint-EDF-Net-EdfAnnotation-IsTimeKeepingAnnotation 'StagPoint.EDF.Net.EdfAnnotation.IsTimeKeepingAnnotation')
  - [Onset](#P-StagPoint-EDF-Net-EdfAnnotation-Onset 'StagPoint.EDF.Net.EdfAnnotation.Onset')
  - [GetSize()](#M-StagPoint-EDF-Net-EdfAnnotation-GetSize 'StagPoint.EDF.Net.EdfAnnotation.GetSize')
- [EdfAnnotationSignal](#T-StagPoint-EDF-Net-EdfAnnotationSignal 'StagPoint.EDF.Net.EdfAnnotationSignal')
  - [#ctor(header)](#M-StagPoint-EDF-Net-EdfAnnotationSignal-#ctor-StagPoint-EDF-Net-EdfSignalHeader- 'StagPoint.EDF.Net.EdfAnnotationSignal.#ctor(StagPoint.EDF.Net.EdfSignalHeader)')
  - [Annotations](#P-StagPoint-EDF-Net-EdfAnnotationSignal-Annotations 'StagPoint.EDF.Net.EdfAnnotationSignal.Annotations')
- [EdfAsciiDateTime](#T-StagPoint-EDF-Net-EdfAsciiDateTime 'StagPoint.EDF.Net.EdfAsciiDateTime')
  - [UseAlternateDateFormat](#P-StagPoint-EDF-Net-EdfAsciiDateTime-UseAlternateDateFormat 'StagPoint.EDF.Net.EdfAsciiDateTime.UseAlternateDateFormat')
- [EdfAsciiInteger](#T-StagPoint-EDF-Net-EdfAsciiInteger 'StagPoint.EDF.Net.EdfAsciiInteger')
- [EdfAsciiString](#T-StagPoint-EDF-Net-EdfAsciiString 'StagPoint.EDF.Net.EdfAsciiString')
- [EdfFile](#T-StagPoint-EDF-Net-EdfFile 'StagPoint.EDF.Net.EdfFile')
  - [Header](#P-StagPoint-EDF-Net-EdfFile-Header 'StagPoint.EDF.Net.EdfFile.Header')
  - [Signals](#P-StagPoint-EDF-Net-EdfFile-Signals 'StagPoint.EDF.Net.EdfFile.Signals')
  - [ReadFrom(filename)](#M-StagPoint-EDF-Net-EdfFile-ReadFrom-System-String- 'StagPoint.EDF.Net.EdfFile.ReadFrom(System.String)')
  - [ReadFrom(file)](#M-StagPoint-EDF-Net-EdfFile-ReadFrom-System-IO-Stream- 'StagPoint.EDF.Net.EdfFile.ReadFrom(System.IO.Stream)')
- [FileType](#T-StagPoint-EDF-Net-StandardTexts-FileType 'StagPoint.EDF.Net.StandardTexts.FileType')
- [MathUtil](#T-StagPoint-EDF-Net-MathUtil 'StagPoint.EDF.Net.MathUtil')
  - [InverseLerp(a,b,value)](#M-StagPoint-EDF-Net-MathUtil-InverseLerp-System-Double,System-Double,System-Double- 'StagPoint.EDF.Net.MathUtil.InverseLerp(System.Double,System.Double,System.Double)')
  - [Lerp(a,b,t)](#M-StagPoint-EDF-Net-MathUtil-Lerp-System-Double,System-Double,System-Double- 'StagPoint.EDF.Net.MathUtil.Lerp(System.Double,System.Double,System.Double)')
- [SignalType](#T-StagPoint-EDF-Net-StandardTexts-SignalType 'StagPoint.EDF.Net.StandardTexts.SignalType')

<a name='T-StagPoint-EDF-Net-EdfAnnotation'></a>
## EdfAnnotation `type`

##### Namespace

StagPoint.EDF.Net

##### Summary

Can be used to store text annotations, time, events, stimuli, etc.

<a name='P-StagPoint-EDF-Net-EdfAnnotation-Annotation'></a>
### Annotation `property`

##### Summary

These annotations may only contain UCS characters (ISO 10646, the 'Universal Character Set', which is
identical to the Unicode version 3+ character set) encoded by UTF-8.

<a name='P-StagPoint-EDF-Net-EdfAnnotation-Duration'></a>
### Duration `property`

##### Summary

Specifies the duration of the annotated event in seconds. If such a specification is not relevant,
Duration can be skipped by setting the value to 0.

<a name='P-StagPoint-EDF-Net-EdfAnnotation-IsTimeKeepingAnnotation'></a>
### IsTimeKeepingAnnotation `property`

##### Summary

TimeKeeping Annotations are automatically stored in the file for purposes of
indicating when each DataRecord begins relative to the start of the file.

<a name='P-StagPoint-EDF-Net-EdfAnnotation-Onset'></a>
### Onset `property`

##### Summary

Specifies the number of seconds by which the onset of the annotated event follows ('+') or precedes ('-')
the startdate/time of the file (the StartTime that is specified in the file header)

<a name='M-StagPoint-EDF-Net-EdfAnnotation-GetSize'></a>
### GetSize() `method`

##### Summary

Returns the number of bytes that would be needed to store this Annotation

##### Parameters

This method has no parameters.

<a name='T-StagPoint-EDF-Net-EdfAnnotationSignal'></a>
## EdfAnnotationSignal `type`

##### Namespace

StagPoint.EDF.Net

##### Summary

An EDF+ Signal that is specially coded to store text annotations, time, events and stimuli
instead of numerical signal information.

<a name='M-StagPoint-EDF-Net-EdfAnnotationSignal-#ctor-StagPoint-EDF-Net-EdfSignalHeader-'></a>
### #ctor(header) `constructor`

##### Summary

Initializes a new instance of the EdfAnnotationSignal class

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| header | [StagPoint.EDF.Net.EdfSignalHeader](#T-StagPoint-EDF-Net-EdfSignalHeader 'StagPoint.EDF.Net.EdfSignalHeader') | And EdfSignalHeader instance containing all of the essential Signal information |

<a name='P-StagPoint-EDF-Net-EdfAnnotationSignal-Annotations'></a>
### Annotations `property`

##### Summary

Contains the full list of Annotations stored in this Signal

<a name='T-StagPoint-EDF-Net-EdfAsciiDateTime'></a>
## EdfAsciiDateTime `type`

##### Namespace

StagPoint.EDF.Net

##### Summary

Stores a fixed-length ASCII string representing a whole number. For consistency

<a name='P-StagPoint-EDF-Net-EdfAsciiDateTime-UseAlternateDateFormat'></a>
### UseAlternateDateFormat `property`

##### Summary

You may encounter legacy EDF files which contain invalid Start Date values ("mm.dd.yy" instead of "dd.mm.yy"),
such as those in the "sleep-heart-health-study-psg-database-1.0.0" dataset. Since it may still be necessary
to read those files, you can set [UseAlternateDateFormat](#P-StagPoint-EDF-Net-EdfAsciiDateTime-UseAlternateDateFormat 'StagPoint.EDF.Net.EdfAsciiDateTime.UseAlternateDateFormat') to TRUE when necessary.
You should otherwise have no other need to change this value.

<a name='T-StagPoint-EDF-Net-EdfAsciiInteger'></a>
## EdfAsciiInteger `type`

##### Namespace

StagPoint.EDF.Net

##### Summary

Stores a fixed-length ASCII string representing a whole number. For consistency

<a name='T-StagPoint-EDF-Net-EdfAsciiString'></a>
## EdfAsciiString `type`

##### Namespace

StagPoint.EDF.Net

##### Summary

Stores a fixed-length ASCII string, which will be right-padded with spaces as necessary to maintain
the fixed-length requirement. In the Header file, these string must only contain the ASCII characters
32..126 (inclusive).

<a name='T-StagPoint-EDF-Net-EdfFile'></a>
## EdfFile `type`

##### Namespace

StagPoint.EDF.Net

##### Summary

Represents a European Data Format file, and is used to read and write EDF files.

<a name='P-StagPoint-EDF-Net-EdfFile-Header'></a>
### Header `property`

##### Summary

Returns the EdfFileHeader instance containing all of the information stored in the EDF Header of this file.

<a name='P-StagPoint-EDF-Net-EdfFile-Signals'></a>
### Signals `property`

##### Summary

Returns the list of all Signals (both Standard signals and Annotation signals) stored in this file.

<a name='M-StagPoint-EDF-Net-EdfFile-ReadFrom-System-String-'></a>
### ReadFrom(filename) `method`

##### Summary

Reads from the file indicated

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| filename | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The full path to the file to be read |

<a name='M-StagPoint-EDF-Net-EdfFile-ReadFrom-System-IO-Stream-'></a>
### ReadFrom(file) `method`

##### Summary

Reads the EDF File information from the provided stream (most often a File Stream)

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| file | [System.IO.Stream](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.IO.Stream 'System.IO.Stream') | The stream which contains the EDF file information to be read |

<a name='T-StagPoint-EDF-Net-StandardTexts-FileType'></a>
## FileType `type`

##### Namespace

StagPoint.EDF.Net.StandardTexts

##### Summary

Standard labels to indicate whether a file contains EDF, EDF+C (Continuous), or EDF+D (Discontinuous) data.

<a name='T-StagPoint-EDF-Net-MathUtil'></a>
## MathUtil `type`

##### Namespace

StagPoint.EDF.Net

##### Summary

Provides a few commonly-used mathematical operations

<a name='M-StagPoint-EDF-Net-MathUtil-InverseLerp-System-Double,System-Double,System-Double-'></a>
### InverseLerp(a,b,value) `method`

##### Summary

Performs an "inverse interpolation" between a and b, returning the time t that
the given value lies between both of those endpoints.

##### Returns



##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| a | [System.Double](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Double 'System.Double') | The start value |
| b | [System.Double](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Double 'System.Double') | The end value |
| value | [System.Double](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Double 'System.Double') | A value between start and end for which to return the interpolation value |

<a name='M-StagPoint-EDF-Net-MathUtil-Lerp-System-Double,System-Double,System-Double-'></a>
### Lerp(a,b,t) `method`

##### Summary

Linearly interpolates between a and b by t.

 The parameter t is clamped to the range [0, 1].
		When t = 0 returns a.
 	When t = 1 return b.
 	When t = 0.5 returns the midpoint of a and b.

##### Returns

The interpolated float result between the two float values.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| a | [System.Double](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Double 'System.Double') | The start value |
| b | [System.Double](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Double 'System.Double') | The end value |
| t | [System.Double](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Double 'System.Double') | The interpolation value (between 0.0 and 1.0) between a and b |

<a name='T-StagPoint-EDF-Net-StandardTexts-SignalType'></a>
## SignalType `type`

##### Namespace

StagPoint.EDF.Net.StandardTexts

##### Summary

Standard labels for common signal types. Not a complete list.
https://www.edfplus.info/specs/edftexts.html#signals"/>
