[MDHEADER]
{FILENAME("meas_", {GROUPVALUE("POL1")}, ".txt")}
{GROUPBY("POL1", "SLIDER", "CHANNEL", "BEAM")}
{ENCODING("UTF-8-BOM")}
[/MDHEADER]
Вид измерения: {FIELD("MeasurementType")}
Антенна: {FIELD("AuxType")}, серийный номер {FIELD("AuxIdentifier")}
Мощность: {FORMAT("#.00 дБм", {FIELD("P")})}
Амплитуда: {FORMAT("#.000 дБ", AMP({VALUE(0, 0, 1, 1, 0, 0, 0, 0)}))}
{TABLE("FREQ", "DATA", "AMP", "0.00", ";")}
