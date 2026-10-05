[MDHEADER]
{FILENAME("meas_", {FIELD("AuxIdentifier")}, "_", {GROUPVALUE("FREQ")}, ".txt")}
{GROUPBY("FREQ")}
{ENCODING("UTF-8-BOM")}
[/MDHEADER]
Вид измерения;{FIELD("MeasurementType")}
Система координат;{FIELD("CoordinateSystem")}
Тип камеры;{FIELD("ChamberType")}
Антенна;{FIELD("AuxType")};{FIELD("AuxIdentifier")}
Начало измерения;{FIELD("DateStart")}
Ось сбора данных;{FIELD("DataAxis")}
Дистанция в точке;{FIELD("MeasurementDistance")}
Ширина полосы ПЧ;{FORMAT("0.0 Гц", {FIELD("IFBW")})}
Мощность;{FORMAT("0.00 дБм", {FIELD("P")})}
Режим синхронизации;{FIELD("SyncModeType")}

{TABLE("POS", "DATA", "AMP", "0.00", ";")}
