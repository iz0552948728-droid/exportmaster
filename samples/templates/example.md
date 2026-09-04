[MDHEADER]
{FILENAME("meas_", {FIELD("MEAS", "NAME")}, "_", POL1, ".csv")}
{GROUPBY(POL1, FREQ, CHANNEL, BEAM)}
{DELIMITER(";")}
{ENCODING("UTF-8")}
[/MDHEADER]
Измерение;{FIELD("MEAS", "NAME")}
Уровень;{FORMAT("#.00 дБм", {FIELD("MEAS", "POW")})}
Амплитуда;{FORMAT("#.000 дБ", AMP({VALUE("DATA", 0, 0, 1, 1, 0, 0, 0, 0)}))}

{TABLE("FREQ", "DATA", "AMP", "0.00")}
