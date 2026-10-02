# TeamSpeak ServerQuery profile

Validated with TeamSpeak Server 3.13.8.

The bridge uses a dedicated ServerQuery login rather than `serveradmin`.

Required operations:

- `use sid=1`
- `clientlist`
- `channellist -topic`
- `clientmove clid=<client> cid=<channel>`

Validated dedicated permissions:

| Permission | Value |
| --- | ---: |
| b_virtualserver_select | 1 |
| b_virtualserver_channel_list | 1 |
| b_virtualserver_client_list | 1 |
| b_channel_join_permanent | 1 |
| i_client_move_power | 100 |

The move power value is deployment-specific: it must satisfy the target client's `i_client_needed_move_power`.

No password or other ServerQuery secret belongs in this repository.
