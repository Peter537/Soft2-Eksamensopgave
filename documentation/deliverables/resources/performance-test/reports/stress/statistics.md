> test info



test suite: `nbomber_default_test_suite_name`

test name: `nbomber_default_test_name`

session id: `2025-12-18_08.07.63_session_987546fa`

> scenario stats



scenario: `stress_test`

  - ok count: `1440`

  - fail count: `0`

  - all data: `0.6` MB

  - duration: `00:01:30`

load simulations:

  - `inject`, rate: `6`, interval: `00:00:01`, during: `00:00:30`

  - `inject`, rate: `12`, interval: `00:00:01`, during: `00:00:30`

  - `inject`, rate: `30`, interval: `00:00:01`, during: `00:00:30`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `1440`, ok = `1440`, RPS = `16`|
|latency (ms)|min = `2.18`, mean = `3.26`, max = `6.3`, StdDev = `0.46`|
|latency percentile (ms)|p50 = `3.21`, p75 = `3.44`, p95 = `4.12`, p99 = `4.82`|
|data transfer (KB)|min = `0.399`, mean = `0.404`, max = `0.407`, all = `0.6` MB|


> status codes for scenario: `stress_test`



|status code|count|message|
|---|---|---|
|OK|1440||


