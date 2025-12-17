> test info



test suite: `nbomber_default_test_suite_name`

test name: `nbomber_default_test_name`

session id: `2025-12-17_12.38.44_session_3e715633`

> scenario stats



scenario: `load_test`

  - ok count: `207`

  - fail count: `0`

  - all data: `0.1` MB

  - duration: `00:00:40`

load simulations:

  - `ramping_inject`, rate: `6`, interval: `00:00:01`, during: `00:00:10`

  - `inject`, rate: `6`, interval: `00:00:01`, during: `00:00:30`

|step|ok stats|
|---|---|
|name|`global information`|
|request count|all = `207`, ok = `207`, RPS = `5.2`|
|latency (ms)|min = `3.31`, mean = `3.71`, max = `4.91`, StdDev = `0.22`|
|latency percentile (ms)|p50 = `3.67`, p75 = `3.84`, p95 = `4.04`, p99 = `4.43`|
|data transfer (KB)|min = `0.399`, mean = `0.502`, max = `1.205`, all = `0.1` MB|


> status codes for scenario: `load_test`



|status code|count|message|
|---|---|---|
|OK|207||


