[MDHEADER]
{FILENAME("meas_", {FIELD("MEAS", "NAME")}, "_", {GROUPVALUE("POL1")}, ".txt")}
{GROUPBY("POL1", "SLIDER", "CHANNEL", "BEAM")}
{ENCODING("UTF-8-BOM")}
[/MDHEADER]
Измерение: {FIELD("MEAS", "NAME")}
Уровень: {FORMAT("#.00 дБм", {FIELD("MEAS", "POW")})}
Амплитуда: {FORMAT("#.000 дБ", AMP({VALUE("DATA", 0, 0, 1, 1, 0, 0, 0, 0)}))}
{TABLE("FREQ", "DATA", "AMP", "0.00", ";")}
