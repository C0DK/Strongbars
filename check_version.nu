let packageVersion = (
  open Strongbars/Strongbars.csproj 
  # TODO: Use 'query xml' when upgrading nu! 
  | from xml 
  | get content 
  | where { $in.tag == 'PropertyGroup' } 
  | get content 
  | flatten 
  | where { $in.tag == 'Version' } 
  | get content.content 
  | flatten -a 
  | first
  )

let tagVersion = git tag | parse "v{major}.{minor}.{patch}" | sort-by major minor patch | last | $"($in.major).($in.minor).($in.patch)"


if $packageVersion != $tagVersion {
  print $"(ansi red)Packageversion/tag version doesnt align! (ansi reset)"
  print $" Package: ($packageVersion)"
  print $" Git tag: ($tagVersion)"
  exit
  error make {
    msg: "Package version misaligned"
  }
} else {
  print $"All ok - Version: ($tagVersion)"
}



