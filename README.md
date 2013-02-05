SourceAnywhere Hosted to Git Importer (SAWHtoGit)
=================================================

This tool will create a `git fast-import` script from an existing [Dynamsoft SourceAnywhere Hosted](http://www.dynamsoft.com/Products/SourceAnywhere-Hosting-Version-Control-Source-Control.aspx) repository. While I haven't tested it, I expect it would also work (with some modification) against their local version as well.

I built this tool as I couldn't find any existing tools to import our source code while preserving history. I'm sharing here in the hopes that others find it useful!

Requirements
------------

To build and run this tool, you'll need the following.

* [SourceAnywhere Hosted COM SDK](http://www.dynamsoft.com/Downloads/SAWHosted_Download.aspx)
* Visual Studio 2012 (with C# support)
* Enough free disk space to replicate your repository a few times over

Configuration
-------------

I didn't implement any command line options for the tool, so you'll have to update the source code directly with your specific configuration options.

Simply look for all of the comments starting with `ACTION:` to find the lines you need to adjust.

You may also need to update Visual Studio's reference to `SAWHSDKLib` to point to your locally installed copy, if it cannot automatically resolve it.

Usage
-----

Once you've updated the source with your credentials, the paths for the exported files, and the projects you wish to export, simply run the following from a command line. Personally, I like to pipe the output to a file for later reference, but you can do as you please.

    SAWHtoGit.exe > sawh_exportlog.txt

If all went well, you should end up with an exact replica of your source tree in the directory specified by the `WorkingDir` parameter in Program.cs. You'll also end up with a `fast-import` script with the filename passed to the `GitExporter` constructor.

### Importing to Git

Assuming you've already got Git installed on your system, open up a Git Shell. **NOTE**: If you care about preserving line-endings exactly as they are, I higlight recommend using a CMD shell for the import, and setting the `autocrlf` git config setting to `false`.

    mkdir <repo directory> && cd <repo directory>
    git init
    git fast-import | cat <path to fast-import script created above>

If all goes well, you should see progress indicators enumerating the changesets. When it's done, you should have an exact replicata of your SourceAnywhere tree in Git!

Contributing
------------

I wrote this as a one-time use tool to migrate our code from SourceAnywhere to GitHub; the migration was sucessful, and there are no plans to make further changes. However, I would love to incorporate changes that would improve the tool for others!

1. Fork it.
2. Create a branch (`git checkout -b my_branch`)
3. Commit your changes (`git commit -am "Added ..."`)
4. Push to the branch (`git push origin my_branch`)
5. Open a [Pull Request][1]

License
-------

This code is licensed under the [MIT license](/LICENSE.TXT).


[1]: http://github.com/dpolivy/SAWHtoGit/pulls