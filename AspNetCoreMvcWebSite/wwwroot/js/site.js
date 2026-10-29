// site.js - all JavaScript for ArtAI (no inline <script> blocks are used anywhere).
// Features in this file:
//   1. Build the three information blocks on the Home page (JavaScript creates the HTML5 elements)
//   2. Mouse over / mouse out on the square blocks (text replaces the picture)
//   3. Show a block only while it is fully inside the browser window (scroll event)
//   4. Preview the chosen picture on the Create and Edit pages
//   5. Prompt Lab page: build a prompt from the user's idea and copy it

"use strict";

// ---------------------------------------------------------------------------
// 1. Data for the Home page blocks.
//    Keeping the text, pictures and links in one array means the same few lines of code
//    build every block, instead of copying HTML three times.
//    "tilesFirst" puts the two squares before the text card (block 2 in the sample layout).
// ---------------------------------------------------------------------------
const homeBlocks = [
    {
        theme: "tile-art",
        heading: "AI Art Generators",
        text: "AI art generators turn a written prompt into a picture in seconds. " +
              "You describe a scene and the model paints it in realistic style, to oil-paint style, comic style or any other style you desire!",
        readMoreUrl: "https://en.wikipedia.org/wiki/Text-to-image_model",
        tilesFirst: false,
        tiles: [
            {
                image: "/images/midjourney.jpg",
                alt: "Midjourney",
                hover: ["Midjourney is an independent research lab whose AI turns text prompts into detailed artwork.", "Midjourney"],
                url: "https://www.midjourney.com/"
            },
            {
                image: "/images/dalle.jpg",
                alt: "DALL-E 3",
                hover: ["DALL-E 3 is OpenAI's text-to-image model and can be used inside ChatGPT.", "OpenAI"],
                url: "https://openai.com/index/dall-e-3/"
            }
        ]
    },
    {
        theme: "tile-music",
        heading: "AI Music Tools",
        text: "AI music tools compose melodies, backing tracks and even full songs with vocals and a varity of instruments! " +
              "Musicians use them to find new ideas, and beginners can make a song without ever having played an instrument.",
        readMoreUrl: "https://en.wikipedia.org/wiki/Music_and_artificial_intelligence",
        tilesFirst: true,
        tiles: [
            {
                image: "/images/suno.jpg",
                alt: "Suno",
                hover: ["Suno creates complete songs, with lyrics and vocals, from a short text description.", "Suno"],
                url: "https://suno.com/"
            },
            {
                image: "/images/aiva.jpg",
                alt: "AIVA",
                hover: ["AIVA composes original, film-style soundtrack music and lets you edit the result.", "AIVA"],
                url: "https://www.aiva.ai/"
            }
        ]
    },
    {
        theme: "tile-writing",
        heading: "AI Writing Tools",
        text: "AI writing tools help authors brainstorm characters, get out of writers block or " +
              "try a different style. The writer stays in charge and decides what to keep.",
        readMoreUrl: "https://en.wikipedia.org/wiki/Generative_artificial_intelligence",
        tilesFirst: false,
        tiles: [
            {
                image: "/images/sudowrite.jpg",
                alt: "Sudowrite",
                hover: ["Sudowrite is an AI writing partner made for fiction writers and storytellers.", "Sudowrite"],
                url: "https://www.sudowrite.com/"
            },
            {
                image: "/images/novelai.jpg",
                alt: "NovelAI",
                hover: ["NovelAI is a subscription service for AI-assisted storytelling and creative writing.", "NovelAI"],
                url: "https://novelai.net/"
            }
        ]
    }
];

// Small helper: create an element, give it a class and (optionally) text.
function createElement(tag, className, text) {
    const element = document.createElement(tag);
    if (className) {
        element.className = className;
    }
    if (text) {
        element.textContent = text;
    }
    return element;
}

// Builds the text card: heading, paragraph and a "Read more" link.
function buildInfoCard(block) {
    const card = createElement("div", "info-card");
    card.appendChild(createElement("h3", "", block.heading));

    const paragraph = createElement("p", "", block.text + " ");
    const readMore = createElement("a", "read-more", "Read more");
    readMore.href = block.readMoreUrl;
    readMore.target = "_blank";
    readMore.rel = "noopener"; // security: the new tab cannot control this page
    paragraph.appendChild(readMore);
    card.appendChild(paragraph);

    return card;
}

// Builds one square block: the picture plus a hidden text layer that appears on mouse over.
function buildTile(tile) {
    const link = createElement("a", "square-block");
    link.href = tile.url;
    link.target = "_blank";
    link.rel = "noopener";

    const image = createElement("img");
    image.src = tile.image;
    image.alt = tile.alt;
    link.appendChild(image);

    const textLayer = createElement("div", "tile-text");
    const paragraph = createElement("p");
    paragraph.appendChild(document.createTextNode(tile.hover[0]));
    paragraph.appendChild(document.createElement("br"));
    paragraph.appendChild(document.createTextNode(tile.hover[1]));
    textLayer.appendChild(paragraph);
    link.appendChild(textLayer);

    // 2. Mouse over / mouse out: CSS shows or hides the text layer depending on this class.
    link.addEventListener("mouseenter", function () {
        link.classList.add("show-text");
    });
    link.addEventListener("mouseleave", function () {
        link.classList.remove("show-text");
    });

    return link;
}

function buildHomeBlocks() {
    const container = document.getElementById("home-blocks");
    if (!container) {
        return; // this is not the Home page
    }

    homeBlocks.forEach(function (block) {
        const wrapper = createElement("article", "reveal-block " + block.theme);
        const card = buildInfoCard(block);
        const tilePair = createElement("div", "tile-pair");
        block.tiles.forEach(function (tile) {
            tilePair.appendChild(buildTile(tile));
        });

        // The order of the card and the squares is different in block 2
        if (block.tilesFirst) {
            wrapper.appendChild(tilePair);
            wrapper.appendChild(card);
        } else {
            wrapper.appendChild(card);
            wrapper.appendChild(tilePair);
        }
        container.appendChild(wrapper);
    });
}

// ---------------------------------------------------------------------------
// 3. Show a block only while it is FULLY inside the viewport.
//    getBoundingClientRect() gives the block's position relative to the window:
//    top >= 0 means its top edge is on screen, bottom <= window height means its bottom edge is too.
// ---------------------------------------------------------------------------
function isFullyInViewport(element) {
    const rect = element.getBoundingClientRect();
    const viewportHeight = window.innerHeight || document.documentElement.clientHeight;

    // A block taller than the window (e.g. a phone turned sideways) can never fit completely,
    // so it counts as visible while it fills the whole window.
    if (rect.height >= viewportHeight) {
        return rect.top <= 0 && rect.bottom >= viewportHeight;
    }
    return rect.top >= 0 && rect.bottom <= viewportHeight;
}

function updateVisibleBlocks() {
    document.querySelectorAll(".reveal-block").forEach(function (block) {
        block.classList.toggle("visible", isFullyInViewport(block));
    });
}

// ---------------------------------------------------------------------------
// 4. Create / Edit pages: show the chosen picture before it is uploaded.
// ---------------------------------------------------------------------------
function setUpImagePreview() {
    const fileInput = document.getElementById("imageFile");
    const preview = document.getElementById("imagePreview");
    if (!fileInput || !preview) {
        return;
    }

    const originalSource = preview.src; // remember the default / current picture

    fileInput.addEventListener("change", function () {
        const file = fileInput.files && fileInput.files[0];
        if (file && file.type.startsWith("image/")) {
            preview.src = URL.createObjectURL(file);
        } else {
            preview.src = originalSource; // no file (or not a picture): go back to the original
        }
    });
}

// ---------------------------------------------------------------------------
// 5. Prompt Lab page (the extra creative page). It joins the user's idea and the chosen
//    style words into one prompt sentence, and lets the user copy it to the clipboard.
//    This code used to be in promptlab.js; it now lives here so all JavaScript is in site.js.
// ---------------------------------------------------------------------------
function setUpPromptLab() {
    const subject = document.getElementById("labSubject");
    const medium = document.getElementById("labMedium");
    const mood = document.getElementById("labMood");
    const lighting = document.getElementById("labLighting");
    const detail = document.getElementById("labDetail");
    const buildButton = document.getElementById("labBuild");
    const copyButton = document.getElementById("labCopy");
    const output = document.getElementById("labOutput");
    const message = document.getElementById("labMessage");

    // If one of the elements is missing this is not the Prompt Lab page, so do nothing.
    if (!subject || !buildButton || !copyButton || !output) {
        return;
    }

    let builtPrompt = ""; // the last prompt that was built

    buildButton.addEventListener("click", function () {
        const idea = subject.value.trim();
        message.textContent = "";

        // Validate the input: an empty idea gives an empty prompt
        if (idea === "") {
            output.textContent = "Please describe what you want to see first.";
            builtPrompt = "";
            subject.focus();
            return;
        }

        const parts = [
            idea,
            medium.value,
            mood.value + " mood",
            lighting.value
        ];
        if (detail.checked) {
            parts.push("highly detailed, sharp focus");
        }

        builtPrompt = parts.join(", ");
        output.textContent = builtPrompt; // textContent (not innerHTML) so typed text can never run as code
    });

    copyButton.addEventListener("click", function () {
        if (builtPrompt === "") {
            message.textContent = "Build a prompt first, then copy it.";
            return;
        }

        // The clipboard API only exists on secure pages (https or localhost) and may be blocked,
        // so the call is wrapped to handle both a missing API and a refused permission.
        if (!navigator.clipboard) {
            message.textContent = "Copying is not available in this browser. Select the text and copy it by hand.";
            return;
        }
        navigator.clipboard.writeText(builtPrompt).then(function () {
            message.textContent = "Copied! Paste it into your favourite AI image tool.";
        }).catch(function () {
            message.textContent = "Could not copy. Select the text and copy it by hand.";
        });
    });
}

// ---------------------------------------------------------------------------
// Start everything once the page has loaded
// ---------------------------------------------------------------------------
document.addEventListener("DOMContentLoaded", function () {
    buildHomeBlocks();
    setUpImagePreview();
    setUpPromptLab();

    window.addEventListener("scroll", updateVisibleBlocks);
    window.addEventListener("resize", updateVisibleBlocks);
    updateVisibleBlocks(); // set the starting state before the user scrolls
});