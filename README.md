# westorrent
_Just because you can, doesn't mean you should._

This is a toy bittorrent client in C#. I am using this to learn. You shouldn't use it in the wild or really even use it as a reference.

## Mock tracker

A very basic tracker is implemented in Perl. I use it to point my torrents to localhost bittorrent clients so I don't embarrass myself fumbling around on a real swarm.

It's read-only, provide your local clients via comma separated ports in env. All peers are `127.0.0.1`.

```
# install dependencies
$ cpanm Plack Bencode

# provide ports via env, comma separated
$ WESTORRENT_LOCAL_PEERS=33854,51413 plackup -r &
Watching app.psgi for file updates.
HTTP::Server::PSGI: Accepting connections at http://0:5000/

$ curl localhost:5000 | xxd
00000000: 6438 3a69 6e74 6572 7661 6c69 3330 6535  d8:intervali30e5
00000010: 3a70 6565 7273 3132 3a7f 0000 0184 3e7f  :peers12:.....>.
00000020: 0000 01c8 d565                           .....e
```

## TODO
- [x] parse torrent file
- [x] get peers via tracker announce
- [x] connect to peer
- [x] download piece from peer
- [ ] save piece in file storage
- [x] connect to peers from tracker
- [x] merge peer list after re-announce
- [x] multiple connections
- [ ] threads??
- [ ] recursive bencode parser for better torrent support
- [ ] multi-file torrents
- [ ] restore progress after restart
