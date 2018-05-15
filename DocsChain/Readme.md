
# DocsChain

> Disclaimer: This software is only at proof-of-concept stage, don't
> pretend this software to be ready for production usage! It works on
> the designed path, but it needs a lot more development. You have been
> warned !


## Project Goal 
DocsChain is a private blockchain to store and certify documents, designed to be very easy to use and deploy.
You can create small peer-to-peer networks of documents contributors. It doesn't have a mining algorithm and so it cannot be used in a public network. However it implements the basic features of a working blockchain.
The chain is stored as a serialised .dc file in the **Config** Directory along with another file, with network peers. The chain doesn't include uploaded documents. Documents are stored into the **FileStorage** Directory using the block Guid.
It is primarly done for PDF storing, but it can work with other extensions (doc,docx,txt,xls,xlsx,png,jpg,jpeg,gif,csv)
## Tech 
Code is based on **Asp .Net core 2.0 + WebApi** 
**Controllers\WebAppController.cs** is for the GUI Action methods
**Controllers\ChainController.cs** is used by WebApis controller, for the intra-nodes communication.
## Dependencies
Only the GUI has dependency with Bootstrap, Jquery, and for PDF viewing, the great plugin [iziModal by Marcelo Dolce](http://izimodal.marcelodolce.com/) .
## Inspiration
[I read an article](https://medium.com/@lhartikk/a-blockchain-in-200-lines-of-code-963cc1cc0e54) on Medium.com by **Lauri Hartikka**, explaining how to build a basic blockchain in Javascript without all the hassles of blockchain players.
I thought it was the time to create my own implementation. Thanks Lauri for the great article !
Here are some Highlights from his article:

> The basic concept of 
> [blockchain](https://en.wikipedia.org/wiki/Blockchain_%28database%29) 
> is quite simple: a distributed database that maintains a continuously
> growing list of ordered records.

![enter image description here](https://i.imgur.com/WOMHIxL.png)


## How DocsChain works
When you first run a **DocsChain** node, it will check locally for **Config** folder and **FileStorage** folder.
If missing, they will be created, along with an empty chain.
GUI has no authentication, you can interact immediately with the node (ARGH :-) ).
The GUI is absolutely basic, built with Bootstrap 4.

### GUI has three pages:

 - **Documents Blocks**
![enter image description here](https://i.imgur.com/NUx42EN.png)

	Here you can preview (only PDF) and download documents in the blockchain.
	You'll see immediately only the **Genesis Block**, built during node bootstrap.
	Moreover, you can upload a pdf documents and add it to the chain.
	If you have built and connected more nodes, the documents will be broadcasted to the other peers.
	Imagine to have 3 offices in your Intranet, and you want the to "certify" a document (data and upload timing).
	Every node can contribute to the Chain, adding any new documents when required. DocsChain limit is now that, without login, I cannot write to the chain who uploaded a document.
	 - **Node Status**![enter image description here](https://i.imgur.com/BIg5H4s.png)


	Here you can find when the DocsChain has been created the first time (Genesis Date) 
You can see the list of Peers connected.

 - **System Functions**
![enter image description here](https://i.imgur.com/ehOnRlx.png)
	In this area you can perform three actions
	

	 - #### Bootstrap

		if you want to **Connect and Sync** to another **DocsChain Node**. Just enter another node IP:PORT (ex: 192.168.1.50:5979) and push the **"Bootstrap Node"** button. You'll see in the logs the chain Sync and network nodes addresses exchange. I tested **DocsChain** with 4 working nodes.
	

	 - #### Add sample PDFs

		Pressing **Add Test Docs** will add 3 sample PDFs to the chain without needing you to upload them manually.
	- #### Full Chain Reset
		Delete the core chain and documents. But **it will not delete the network peers** list 
		(got to develop it).


# How to run the POC

 1. ## The easy way, Visual Studio 2017

Download the project and just Run it with **Visual Studio 2017**.
If everything works fine, you'll have a consolle opening with logs and the browser will open on the Chain Node Url . If browser is not opening , go to the url **http://[YourNodeIP]:[YourNodePort]**
If you want customize **Node Name, Listening IP or Port**, edit **appsettings.json** file:

     "NodeIdentification": {
        "Name": "Node 10",
        "ListeningPort": "5979",
        "IPEndpoint": "http://192.168.1.147:5979"
    	}

Keep **ListeningPort** and the **IPEndpoint** port the same (this is a POC, it'll be optimized later...)
It is mandatory to use the real IP of Network Interface (**ipconfig /all** on command line) if you want to make the POC work in your intranet on different machines.
You cannot  use **localhost** or **127.0.0.1** here. You can do it (not tested) only if you're going to run **multiple DocsChain nodes** on the same machine . 

 2. ## Multiple Nodes Way, with command line
If you are a brave and you want to showcase **DocsChain** with **multiple nodes** (on the same machine, never tried on different machines), I suggest the following approach (*remember to replace the IP below with your own IP and desired port*):
 - Right Click on Project Root Node and Publish to a Folder.  (es:c:\DocsChainBase\)
 - Clone the Folder as c:\DocsChain5010
 - Clone the Folder as c:\DocsChain5020
 - Open c:\DocsChain5010\appsettings.json and set

     "NodeIdentification": {
	               "Name": "Node 5010",
                "ListeningPort": "5010",
                "IPEndpoint": "http://192.168.1.147:5010"
            	}

  

 - Open c:\DocsChain5020\appsettings.json and set

     "NodeIdentification": {
        "Name": "Node 5020",
        "ListeningPort": "5020",
        "IPEndpoint": "http://192.168.1.147:5020"
    	}

 - Now you have 2 configured nodes .

 - Open a Command Line 1 and cd \DocsChain5010\

 - launch the first node with: `dotnet DocsChain.dll` 

 - Open the browser and go to http://192.168.1.147:5010/

 - Add some documents or use the System Menù to add them to the chain (5010)

 - Go to \DocsChain5020\

 - run `dotnet DocsChain.dll`.The second node will boot

 - Browse to http://localhost:5020/

 - Go to the System menu

 - Enter the First Node Boot Url without http (es: **192.168.1.147:5010**)

 - If everything has gone fine, **you have 2 synced DocsChain nodes** .

You can repeat with other nodes and boot from any network connected live node.

Now you can try to upload a document on any node, and it will be broadcasted to the others in the network.
POC End :-)

### Troubleshooting
How to restart from the beginning when you are going crazy:

 - stop a node
 - delete **Config** Folder and **FileStorage** Folder
 - restart the node

### Missing Features

 - User Authentication and user management.
 - Packaging the app as a Windows Service
 - Secure data transfer between nodes with HTTPS.
 - Docs Encription on file system.
 - File watcher to keep an eye on documents stored on local filesystem.
 - Auto-Recover documents from other nodes if found a file corrupted (different Hash)
 - General recovery of BlockChain if something is going wrong
 - Supporting more native files viewer in the GUI
 - Search along documents feature
 - ...

### "Hidden" Command line switches... 
`dotnet DocsChain.dll --boot http://localhost:5030` (To boot a node without using the GUI)
`dotnet DocsChain.dll --create-test-chain true` (boot and add immediately sample documents to the chain)

## Contributing Guidelines
Really, this is my first open source projects. I don't know if it will catch someone interest, and I never contributed to others projects. If you find it interesting and want to contribute, drop me a line.

This project is licensed under the terms of the MIT license.