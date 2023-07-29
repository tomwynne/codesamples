import { MouseEvent } from "react";

function ListGroup3() {
  let items = ["New York", "San Francisco", "Tokyo", "London", "Paris"];

  // event handler
  const handleClick = (event: MouseEvent) => {
    console.log(event.target);
  };
  return (
    <>
      <h1>List</h1>
      {items.length === 0 && <p>No items found</p>}
      <ul className="list-group">
        {items.map((item, index) => (
          <li key={item} className="list-group-item" onClick={handleClick}>
            {item}
          </li>
        ))}
      </ul>
    </>
  );
}

export default ListGroup3;
