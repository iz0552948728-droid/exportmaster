[MDHEADER]
{FILENAME("meas_", {GROUPVALUE("POL1")}, "_", {GROUPVALUE("FREQ")}, ".csv")}
{GROUPBY("POL1", "FREQ")}
{ENCODING("UTF-8-BOM")}
[/MDHEADER]
Вид измерения: {FIELD("MeasurementType")}
Система координат: {FIELD("CoordinateSystem")}
Тип камеры: {FIELD("ChamberType")}
Антенна: {FIELD("AuxType")}; {FIELD("AuxIdentifier")}
Начало измерения: {FIELD("DateStart")}
Поляризация: {GROUPVALUE("POL1")}
Частота: {GROUPVALUE("FREQ")}
Ширина полосы ПЧ: {FORMAT("0.0 Гц", {FIELD("IFBW")})}
Мощность: {FORMAT("0.00 дБм", {FIELD("P")})}

{TABLE("POS", "DATA", "AMP", "0.000", ";")}
