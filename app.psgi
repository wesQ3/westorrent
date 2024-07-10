use v5.32; use warnings;
use Bencode 'bencode';

sub tracker_body {
   my $ports = $ENV{WESTORRENT_LOCAL_PEERS} || '';
   my $peers = join '', map { pack('C4n', 127, 0, 0, 1, $_) } split /,/, $ports;

   bencode({
      interval => 30,
      peers => $peers,
   });
}

my $app = sub {
   if (!$ENV{WESTORRENT_LOCAL_PEERS}) {
      return [
         500,
         ['Content-Type' => 'text/plain'],
         ['NO PEERS IN ENV'],
      ]
   }

   [
      200,
      ['Content-Type' => 'text/plain'],
      [tracker_body()],
   ]
};
