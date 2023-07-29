import { useState } from "react";

// { items: [], heading: string }
interface Props {
  items: string[];
  heading: string;
}

function ListGroup8(props: Props) {
  // Hook -
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [name, setName] = useState("Tom");

  return (
    <>
      <h1>{props.heading}</h1>
      {props.items.length === 0 && <p>No items found</p>}
      <ul className="list-group">
        {props.items.map((item, index) => (
          <li
            key={item}
            className={
              selectedIndex === index
                ? "list-group-item active"
                : "list-group-item"
            }
            onClick={() => {
              setSelectedIndex(index);
            }}
          >
            {item}
          </li>
        ))}
      </ul>
    </>
  );
}

export default ListGroup8;
