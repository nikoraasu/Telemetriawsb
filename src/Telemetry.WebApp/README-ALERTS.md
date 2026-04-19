# Alerty InfluxDB

Utwórz ręcznie regułę alertową w InfluxDB i ustaw webhook na:

`http://localhost:5003/api/webhook/influx`

W payloadzie możesz wysyłać pola takie jak:

- `ruleName`
- `message`
- `measurement`
- `location`
- `value`
