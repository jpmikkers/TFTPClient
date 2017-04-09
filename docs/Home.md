**Project Description**

Managed TFTP client implementation, written in C#. Supports:
- IPv4 and IPv6
- correct retry behavior.
- TFTP options: block size, transfer size, and timeout.
- unlimited transfer sizes
- permissive license for commercial use.
- contains an easy to use library and a command line client

See the wikipage [Documentation](Documentation) for more information on how to use the library from your own code.

Here's the help page from the command line utility:

{{
TFTPClient 1.0.0
Transfers files to and from a remote computer running the TFTP service.

Usage: TFTPClient [options](options)+ host[:port](_port)

      --get                  get a file from remote to local
      --put                  put a file from local to remote
      --local=VALUE          local filename
      --remote=VALUE         remote filename
      --serverport=VALUE     override server port (default: 69)
      --blocksize=VALUE      set blocksize (default: 512)
      --timeout=VALUE        set response timeout [s](s) (default: 2)
      --retries=VALUE        set maximum retries (default: 5)
      --verbose              generate verbose tracing
      --ipv6                 resolve hostname to an ipv6 address
      --dontfragment         don't allow packet fragmentation (default: allowed)

      --silent               don't show progress information
      --ttl=VALUE            set time to live
  -?, -h, --help             show help
}}

This project is maintained on my own time. If you find it useful, please consider making a small donation:

**BitCoin:** 1AQGabuTggsiDy3YFhMg86LWRiw5mdrU8L

