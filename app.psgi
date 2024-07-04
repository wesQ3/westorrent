use v5.32; use warnings;
use Bencode 'bencode';

sub tracker_body {
   my $peers = $ENV{WESTORRENT_LOCAL_PEERS} || '';
   my @peers = map +{ ip => '127.0.0.1', port => $_ }, split /,/, $peers;

   say STDERR 'Peers:';
   say STDERR "  $_->{ip}:$_->{port}" for @peers;
   bencode({
      interval => 60 * 5,
      peers => \@peers,
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
