import { useState } from "react";

// { items: [], heading: string }
interface Props {
  items: string[];
  heading: string;
}

// destructured props so don't need props.item, props.heading
function ListGroup9({ items, heading }: Props) {
  // Hook -
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [name, setName] = useState("Tom");

  return (
    <>
      <h1>{heading}</h1>
      {items.length === 0 && <p>No items found</p>}
      <ul className="list-group">
        {items.map((item, index) => (
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

export default ListGroup9;
