> test info



test suite: `nbomber_default_test_suite_name`

test name: `nbomber_default_test_name`

session id: `2025-12-18_08.14.69_session_35cb3e2e`

> scenario stats



scenario: `load_test`

  - ok count: `1978`

  - fail count: `0`

  - all data: `1.5` MB

  - duration: `00:06:00`

load simulations:

  - `ramping_inject`, rate: `6`, interval: `00:00:01`, during: `00:01:00`

  - `inject`, rate: `6`, interval: `00:00:01`, during: `00:05:00`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `1978`, ok = `1978`, RPS = `5.5`|
|latency (ms)|min = `2.44`, mean = `3.39`, max = `9.19`, StdDev = `0.36`|
|latency percentile (ms)|p50 = `3.38`, p75 = `3.52`, p95 = `3.82`, p99 = `4.16`|
|data transfer (KB)|min = `0.399`, mean = `0.792`, max = `2.468`, all = `1.5` MB|


> status codes for scenario: `load_test`



|status code|count|message|
|---|---|---|
|OK|1978||


